import os
import wave
import json
import pyaudio
from vosk import Model, KaldiRecognizer

def recognize_speech_from_microphone():
    # Path to the downloaded Vosk model for Brazilian Portuguese
    model_path = "vosk-model-small-pt-0.3"
    
    # Check if the model path exists
    if not os.path.exists(model_path):
        print("Model path does not exist.")
        return

    # Load the Vosk model
    model = Model(model_path)
    recognizer = KaldiRecognizer(model, 16000)

    # Initialize PyAudio
    audio = pyaudio.PyAudio()
    stream = audio.open(format=pyaudio.paInt16, channels=1, rate=16000, input=True, frames_per_buffer=8192)
    stream.start_stream()

    print("Diga algo!")

    while True:
        data = stream.read(4096, exception_on_overflow=False)
        if recognizer.AcceptWaveform(data):
            result = recognizer.Result()
            result_json = json.loads(result)
            print("Você disse: " + result_json['text'])

if __name__ == "__main__":
    recognize_speech_from_microphone()
