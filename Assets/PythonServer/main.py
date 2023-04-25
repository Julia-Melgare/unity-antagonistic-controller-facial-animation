import zmq
import numpy as np
import tensorflow as tf

#TODO: load model at start of running server and run prediction on a placeholder image for warmup
context = zmq.Context()
socket = context.socket(zmq.REP)
socket.bind("tcp://*:5555")

def run_inference(image):
    graph_def = tf.GraphDef()

    with tf.gfile.Open("model_mit1003_gpu.pb", "rb") as file:
        graph_def.ParseFromString(file.read())
        tf.import_graph_def(graph_def, name='')

    #process image
    image = tf.image.convert_image_dtype(image, dtype=tf.float32)
    image = tf.expand_dims(image, 0)
    image = tf.clip_by_value(image[0], 0.0, 255.0)
    #jpeg = postprocess_saliency_map(predicted_maps[0])
    output_layer = 'output:0'
    input_node = 'input:0'

    with tf.Session() as sess:
        image_input = sess.run(image)
        try:
            prob_tensor = sess.graph.get_tensor_by_name(output_layer)
            predictions = sess.run(prob_tensor, {input_node: image_input})
            saliency_map = postprocess_saliency_map(predictions[0])
            output = sess.run(saliency_map)
        except KeyError:
            print ("Couldn't find classification output layer: " + output_layer + ".")
            print ("Verify this a model exported from an Object Detection project.")
            exit(-1)
        
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
