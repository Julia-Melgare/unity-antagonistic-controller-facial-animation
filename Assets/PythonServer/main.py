import zmq
import numpy as np
import tensorflow as tf

context = zmq.Context()
socket = context.socket(zmq.REP)
socket.bind("tcp://*:5555")

#while True:
    #receive image bytes
    #pass them as input to model
    #return processed saliency map