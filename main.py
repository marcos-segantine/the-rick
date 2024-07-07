import os
import json
import pyaudio
from vosk import Model, KaldiRecognizer

import pyttsx3
from openai import OpenAI

from dotenv import load_dotenv

load_dotenv() 

engine = pyttsx3.init()
engine.setProperty("rate", 120)

client = OpenAI(api_key=os.getenv("OPEAN_AI_KEY"))

def recognize_speech_from_microphone():
    # Path to the downloaded Vosk model
    model_path = "vosk-model-small-en-us-0.15"
    
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


    os.system("clear");
    print("Say something!")

    while True:
        data = stream.read(4096, exception_on_overflow=False)
        if recognizer.AcceptWaveform(data):
            result = recognizer.Result()
            result_json = json.loads(result)
            print("You said: " + result_json['text'])
            
            if(result_json['text'] == ""):
              continue
            
            completion = client.chat.completions.create(
              model="gpt-3.5-turbo",
              messages=[
                {"role": "system", "content": "You are a robot called Rick and your function is talk with the people."},
                {"role": "system", "content": "You was made by a professor called Marcos Segantine. You and he are in the city Nova ponte that is located in Minas Geras state"},
                {"role": "system", "content": "Do not say 'How can I assist you today'"},
                {"role": "user", "content": result_json['text']}
              ]
            )
            
            print(completion.choices[0].message.content)
            
            text_to_speech(completion.choices[0].message.content)

def text_to_speech(text):

    engine.say(text)
    engine.runAndWait()


if __name__ == "__main__":
    recognize_speech_from_microphone()
