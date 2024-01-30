import zmq
import struct
import gc
import cv2
import numpy as np
import tensorflow as tf

context = zmq.Context()
socket = context.socket(zmq.REP)
socket.bind("tcp://*:5555")

graph_def = tf.GraphDef()
with tf.gfile.Open("model_mit1003_gpu.pb", "rb") as file:
    graph_def.ParseFromString(file.read())

input_plhd = tf.placeholder(tf.float32, (None, None, None, 3))

[predicted_maps] = tf.import_graph_def(graph_def,
                                       input_map={"input": input_plhd},
                                       return_elements=["output:0"])
# load saccadic map and bias map
saccadic_map = cv2.imread('saccadic_bias.png')
horizontal_map = cv2.imread('horizontal_bias.png')

saccadic_map = cv2.cvtColor(saccadic_map, cv2.COLOR_BGR2GRAY)
horizontal_map = cv2.cvtColor(horizontal_map, cv2.COLOR_BGR2GRAY)

# set map weights
saliency_weight = 0.9
saccadic_weight = 0.85
horizontal_weight = 0.1
with tf.Session() as sess:
    #run warm-up inference
    warmup_input = cv2.imread('input.png')
    input_img = cv2.cvtColor(warmup_input, cv2.COLOR_BGR2RGB)
    input_img = input_img[np.newaxis, :, :, :]
    saliency = sess.run(predicted_maps,
                        feed_dict={input_plhd: input_img})
    print("Finished warm-up")

    while True:
        #receive image bytes
        bytes_received = socket.recv(102400)
        #pass them as input to model
        nparr = np.fromstring(bytes_received, np.uint8)
        image = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        #process image
        input_img = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
        input_img = input_img[np.newaxis, :, :, :]

        saliency = sess.run(predicted_maps,
                            feed_dict={input_plhd: input_img})

        saliency = saliency.squeeze(0)
        saliency = cv2.resize(saliency, dsize=(256,256), interpolation=cv2.INTER_CUBIC) 
        saliency = saliency.astype("float32")/255 
        # apply saliency weight
        saliency = (saliency * saliency_weight) + (1.0 - saliency_weight)   
        # get saccadic map
        saccadic_map = saccadic_map.astype("float32")/255
        # apply saccadic weight
        saccadic_map = (saccadic_map * saccadic_weight) + (1.0 - saccadic_weight)
        # get horizontal bias map
        horizontal_map = horizontal_map.astype("float32")/255
        # apply horizontal weight   
        horizontal_map = (horizontal_map * horizontal_weight) + (1.0 - horizontal_weight)
        # maps as probability distribution
        sum_saliency = np.sum(saliency)
        sum_saccadic = np.sum(saccadic_map) 
        sum_horizontal = np.sum(horizontal_map)  
        # normalize distributions
        #saliency = saliency/sum_saliency
        #saccadic_map = saccadic_map/sum_saccadic
        #horizontal_map = horizontal_map/sum_horizontal

        # create the fixation probability map
        fixation_map = saccadic_map * saliency
        fixation_map = fixation_map * horizontal_map
        fixation_proba = fixation_map.flatten()

        # save fixation map for debug
        fixation_norm = (fixation_map-np.min(fixation_map))/(np.max(fixation_map)-np.min(fixation_map))
        fixation_map_img = cv2.cvtColor(fixation_norm.squeeze(), cv2.COLOR_GRAY2BGR)
        fixation_map_img = np.uint8(fixation_map_img*255)
        cv2.imwrite('fixation_map.png', fixation_map_img)

        # sample 20 random pixels from map
        #samples_index = np.random.choice(range(0, len(fixation_proba)), 20)
        #samples_prob = [(fixation_proba[x],x) for x in samples_index]
        #samples_prob.sort(reverse=True)
        #fixation_sample = samples_prob[0][1]

        fixation_sample = np.argmax(fixation_proba)
        #uv_coordinates = (fixation_sample % 256,  (int)(fixation_sample / 256))
        print(fixation_sample)

        # get fixation time based on gamma distribution
        fixation_time = np.random.gamma(1.2394, 0.1880, 1)[0]
        fixation_time = fixation_time.item()
        print(fixation_time)

        #return fixation sample index and fixation time
        byte_encode = fixation_sample.tobytes()
        byte_encode += struct.pack('!d', fixation_time)
        socket.send(byte_encode)
        gc.collect()