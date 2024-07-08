import os
import json
import pyaudio
from vosk import Model, KaldiRecognizer
import boto3
import pygame
import io
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
            return completion.choices[0].message.content            

def text_to_speech(text):
    aws_access_key_id = os.getenv("AWS_ACCESS_KEY_ID")
    aws_secret_access_key = os.getenv("AWS_SECRET_ACCESS_KEY")

    polly = boto3.client('polly', 
                     region_name='sa-east-1', 
                     aws_access_key_id=aws_access_key_id, 
                     aws_secret_access_key=aws_secret_access_key)

    response = polly.synthesize_speech(
        Text=text,
        OutputFormat='mp3',
        VoiceId='Salli'
    )
    
    audio_stream = response['AudioStream'].read()
    return audio_stream

def play_audio(audio_stream):
    pygame.mixer.init()
    
    audio_file = io.BytesIO(audio_stream)
    
    pygame.mixer.music.load(audio_file, 'mp3')
    
    pygame.mixer.music.play()

    while pygame.mixer.music.get_busy():
        pygame.time.Clock().tick(10)

if __name__ == "__main__":
  while True:
      text = recognize_speech_from_microphone()
      audio_stream = text_to_speech(text)
      play_audio(audio_stream)
