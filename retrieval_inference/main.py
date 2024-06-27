import zmq
from inference import get_retrieval_model, predict
import os
# from keras.models import load_model

# model = load_model('model.h5')

model = get_retrieval_model()

context = zmq.Context()
socket = context.socket(zmq.REP)
socket.bind("tcp://*:5555")


while True:
    # bytes_received = socket.recv(3136)
    # array_received = np.frombuffer(bytes_received, dtype=np.float32).reshape(28,28)

    message = socket.recv()
    print("Received request: %s" % message)
    if os.path.exists(message):
        # socket.send_string("Received and exists!")
        pred = predict(model, message)
        print("Predict shape: " + pred)
        socket.send_string(pred)
    else:
        socket.send_string("Received but not exists!")
