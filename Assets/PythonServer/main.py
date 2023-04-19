import zmq
import numpy as np
import tensorflow as tf

context = zmq.Context()
socket = context.socket(zmq.REP)
socket.bind("tcp://*:5555")

def run_inference(image):
    image = tf.image.convert_image_dtype(image, dtype=tf.float32)
    image = tf.clip_by_value(image[0], 0.0, 255.0)
    graph_def = tf.GraphDef()

    with tf.gfile.Open("model_mit1003_gpu.pb", "rb") as file:
        graph_def.ParseFromString(file.read())

    [predicted_maps] = tf.import_graph_def(graph_def, input_map={"input": image}, return_elements=["output:0"])
    jpeg = postprocess_saliency_map(predicted_maps[0])

    with tf.Session() as sess:
        output = sess.run(jpeg)
    return output

def postprocess_saliency_map(saliency_map):
    saliency_map *= 255.0

    saliency_map = tf.round(saliency_map)
    saliency_map = tf.cast(saliency_map, tf.uint8)

    saliency_map_jpeg = tf.image.encode_jpeg(saliency_map, "grayscale", 100)

    return saliency_map_jpeg

while True:
    #receive image bytes
    bytes_received = socket.recv(102400)
    #pass them as input to model
    image = tf.image.decode_png(bytes_received, channels=3)
    #return processed saliency map
    bytes_to_send = run_inference(image)
    socket.send(bytes_to_send)
