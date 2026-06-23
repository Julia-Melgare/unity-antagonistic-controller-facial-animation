import cv2
import zmq
import numpy as np
import math
from collections import deque

def merge_close_components(mask, merge_dist=10.0):
    
    num_labels, labels, stats, centroids = cv2.connectedComponentsWithStats(mask, connectivity=8)
    if num_labels <= 1:
        return labels, []

    # Build adjacency (distance < merge_dist)
    centroids = np.array(centroids)
    diff = centroids[:, None, :] - centroids[None, :, :]
    dist_sq = np.sum(diff**2, axis=-1)
    adjacency = (dist_sq < merge_dist**2) & (dist_sq > 0)

    # Simple union-find (disjoint set)
    parent = np.arange(num_labels)
    def find(x):
        while parent[x] != x:
            parent[x] = parent[parent[x]]
            x = parent[x]
        return x
    def union(x, y):
        rx, ry = find(x), find(y)
        if rx != ry:
            parent[ry] = rx

    # Merge connected centroids
    for i in range(num_labels):
        for j in np.where(adjacency[i])[0]:
            union(i, j)

    # Re-label components
    groups = {}
    new_labels = np.zeros_like(labels)
    next_id = 1
    merged_stats = []

    for i in range(1, num_labels):
        root = find(i)
        if root not in groups:
            groups[root] = next_id
            next_id += 1
        new_labels[labels == i] = groups[root]

    # Compute new stats
    for gid in range(1, next_id):
        mask_i = (new_labels == gid)
        if not np.any(mask_i):
            continue
        y, x = np.nonzero(mask_i)
        area = mask_i.sum()
        cx, cy = x.mean(), y.mean()
        x0, y0, x1, y1 = x.min(), y.min(), x.max(), y.max()
        if x0 == x1:
            x1 += 1
        if y0 == y1:
            y1 += 1
        merged_stats.append({
            "id": gid,
            "area": int(area),
            "centroid": (cx, cy),
            "bbox": (x0, y0, x1 - x0, y1 - y0)
        })

    return new_labels, merged_stats
      
class MotionScaleEstimator:
    def __init__(self, window_size=60, percentile=55, min_scale=1e-3):
        self.window = deque(maxlen=window_size)
        self.percentile = percentile
        self.min_scale = min_scale

    def update(self, value):
        self.window.append(value)

    def get_scale(self):
        if len(self.window) < 5:
            return self.min_scale

        scale = np.percentile(self.window, self.percentile)
        return max(scale, self.min_scale)

class MotionSaliencyEstimator:
    def __init__(self):
        self.relative_speed_scale_estim = MotionScaleEstimator()
        self.speed_changes_scale_estim = MotionScaleEstimator()
    
    def calculate_average_velocity(self, image, bbox):
        # OpenCV reads images as BGR
        x,y,w,h = bbox
        obj_region = image[y:y+h, x:x+w]
        non_black_mask = np.any(obj_region != 0, axis=2)
        pixels = obj_region[non_black_mask]
        
        if len(pixels) <=0:
            return (0,0), 0

        mean_vector = (np.mean(pixels[:, 2]), np.mean(pixels[:, 1])) # Accessing R (2) and G (1) channels
        mean_mag = np.linalg.norm(mean_vector)
        return mean_vector, mean_mag

    def normalize(self, value, scale):
        return value / (value + scale)

    def update_motion_saliency(self, image, objects, alpha=0.3):
        # calculate scene speed average
        object_speeds = [t.of_magnitude for t in objects]
        scene_mean = np.mean(object_speeds) if object_speeds != [] else 0.0

        # update own object's Exponentially Weighted Moving Average and relative speed in comparison to the scene
        for object in objects:
            object.of_vector, object.of_magnitude = self.calculate_average_velocity(image, object.bbox)
            object.avg_speed = (1 - alpha) * object.avg_speed + alpha * object.of_magnitude

            # calculate motion saliency score
            relative_speed = max(0.0, object.of_magnitude - scene_mean)
            speed_change = abs(object.of_magnitude - object.avg_speed)

            # update dynamic normalization scale estimation
            self.relative_speed_scale_estim.update(relative_speed)
            self.speed_changes_scale_estim.update(speed_change)

            # normalize values
            area_norm = self.normalize(object.area, 100) # using 100 pixels as scale for area
            relative_speed_norm = self.normalize(relative_speed, self.relative_speed_scale_estim.get_scale())
            speed_change_norm = self.normalize(speed_change, self.speed_changes_scale_estim.get_scale())

            #print(f"speed change score:{speed_change_norm}, relative speed score:{relative_speed_norm}, area score:{area_norm}")

            object.motion_saliency_score = 0.4 * speed_change_norm + 0.4 * relative_speed_norm + 0.2 * area_norm
            
class CentroidTracker(object):
    def __init__(self, id, area, centroid, bbox):
        self.id = id
        self.area = area
        self.centroid = centroid
        self.bbox = bbox

        self.of_vector = (0,0)
        self.of_magnitude = 0
        self.centroid_velocity = (0,0)
        self.last_centroid_pos = (0,0)

        self.avg_speed = 0
        self.motion_saliency_score = 0

        self.hits = 0
        self.lost = 0

    def update(self, area, centroid, bbox):   
        self.area = area
        self.last_centroid_pos = self.centroid
        self.centroid = centroid
        self.bbox = bbox
        self.centroid_velocity = np.subtract(self.centroid, self.last_centroid_pos)

        self.hits += 1
        self.lost = 0

    def predict(self):
        self.lost +=1
        self.bbox = (int(self.bbox[0] + self.centroid_velocity[0]), int(self.bbox[1] + self.centroid_velocity[1]), self.bbox[2], self.bbox[3])

    def __str__(self):
        return f"[id:{self.id}, area:{self.area}, centroid:{self.centroid}, bbox:{self.bbox}, of_v:{self.of_vector}, of_mag:{self.of_magnitude}, avg_speed: {self.avg_speed}, centroid_v:{self.centroid_velocity}, hits:{self.hits}, lost:{self.lost}, motion saliency score: {self.motion_saliency_score}]"

class Tracker(object):
    def __init__(self, max_age=1, min_hits=3, dist_threshold=3.0, max_id_threshold=100):
        self.max_age = max_age
        self.min_hits = min_hits
        self.dist_threshold = dist_threshold
        self.max_id_threshold = max_id_threshold
        self.trackers = []

        # ID management
        self.ids = set(range(1, max_id_threshold))
        self.used_ids = set()

    def get_next_id(self):
        next_id =  min(self.ids - self.used_ids)
        self.used_ids.add(next_id)
        return next_id
    
    def release_id(self, id):
        self.used_ids.remove(id)

    def update(self, objects):
               
        match_candidates = []
        for i in range(0, len(objects)):
            c1 = objects[i]['centroid']
            for j in range(0, len(self.trackers)):
                c2 = self.trackers[j].centroid
                centroid_dist = math.sqrt((c2[0] - c1[0])**2 + (c2[1] - c1[1])**2)
                if centroid_dist <= self.dist_threshold:
                    match_candidates.append((centroid_dist, i, j))

        match_candidates.sort(key=lambda x: x[0])
        matched_objects = set()
        matched_trackers = []

        for d, i, j in match_candidates:
            if i not in matched_objects and j not in matched_trackers:
                self.trackers[j].update(objects[i]['area'], objects[i]['centroid'], objects[i]['bbox'])
                matched_objects.add(i)
                matched_trackers.append(self.trackers[j])
    
    
        for tracker in self.trackers:
            if tracker not in matched_trackers:            
                tracker.predict()
    
        for i in range(len(objects)):
            if i not in matched_objects:
                new_tracker = CentroidTracker(self.get_next_id(), objects[i]['area'], objects[i]['centroid'], objects[i]['bbox'])
                self.trackers.append(new_tracker)
        
        detections = []
        for tracker in self.trackers:
            if tracker.lost >= self.max_age:
                self.release_id(tracker.id)
                self.trackers.remove(tracker)
                continue
            if tracker.hits >= self.min_hits:
                detections.append(tracker)

        return detections
    
def prepare_response(detections):
    res = ""
    for obj in detections:
        res += f"{{\"id\": {obj.id}, \"centroid\": {list(obj.centroid)}, \"score\":{obj.motion_saliency_score}}}\n"
    return res

context = zmq.Context()
socket = context.socket(zmq.REP)
socket.bind("tcp://*:5556")

my_tracker = Tracker(max_age=3, min_hits=3, dist_threshold=15)
motion_sal_estim = MotionSaliencyEstimator()
detections = []

while True:
    #receive image bytes
    bytes_received = socket.recv(102400)
    nparr = np.fromstring(bytes_received, np.uint8)
    image = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    _, mask = cv2.threshold(gray, 1, 255, cv2.THRESH_BINARY)
    merged_labels, merged_stats = merge_close_components(mask, merge_dist=15.0)
    # update tracker
    detections = my_tracker.update(merged_stats)
    # process OF related stuff
    motion_sal_estim.update_motion_saliency(image, detections)
    #return response
    response = prepare_response(detections)
    print(response)
    print("sending response...")
    socket.send_string(response)