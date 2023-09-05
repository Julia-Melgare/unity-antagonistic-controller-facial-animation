import zmq
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

        saliency = cv2.cvtColor(saliency.squeeze(),
                                cv2.COLOR_GRAY2BGR)

        saliency = np.uint8(saliency * 255)

        #return processed saliency map
        img_encode = cv2.imencode('.jpg', saliency)[1]
        data_encode = np.array(img_encode)
        byte_encode = data_encode.tobytes()
        socket.send(byte_encode)
        gc.collect()
