#include <IRremote.hpp>

#define IrAddress 0x0

enum Pin{
  NONE,
  LED,
  IR,
  BTN,
  POT,
};

Pin pins[26];

int inputPins[26], numInputPins, inputPinsPrevVal[26];

void setup() {
  // put your setup code here, to run once:
  Serial.begin(9600);
  while (!Serial){;}
  
  Reset();
  Serial.write(0xFC); // Request Configuration
}

void loop() {
  // put your main code here, to run repeatedly:

  for (int i = 0; i < numInputPins; i++){
    if (pins[inputPins[i]] == BTN){
      int val = digitalRead(CleanPin(inputPins[i]));
      if (val != inputPinsPrevVal[i]){
        inputPinsPrevVal[i] = val;
        Serial.write((inputPins[i] << 3) | val << 1);
      }
    }
    else if (pins[inputPins[i]] == POT){
      // TODO: implement potentiometer
    }
  }


  if (Serial.available() >= 1) {
    byte command;
    Serial.readBytes(&command, 1);

    bool configure = (command & 0x80) >> 7; // Update type 
    int pin = (command & 0x7C) >> 2;

    if (configure){

      bool input = !((command & 0x02) >> 1); // Pinmode
      int type = command & 0x01;

      if (pin == 0x1F){ // Reset Configuration
        Reset();
      }

      else if (!input){ // Output
        if (type == 0){ // LED
            pins[pin] = LED;
            pinMode(CleanPin(pin), OUTPUT);
          }
          else{ // IR
            pins[pin] = IR;
            IrSender.begin(CleanPin(pin));
          } 
          
      }

      else if (input && type == 0){ // Input Remove
        pins[pin] = NONE;
        bool movePin = false;
        for (int i = 0; i < numInputPins; i++){
          if (movePin){
            inputPins[i - 1] = inputPins[i];
          }

          if (inputPins[i] == pin){
            movePin = true;
          }
        }
        numInputPins--;
      }

      else if (pin <= 13){ // Button (D0 - D13)
        pins[pin] = BTN;
        inputPins[numInputPins] = pin;
        numInputPins++;

        pinMode(pin, INPUT);
      }

      else if (pin <= 19){ // Potetiometer (A0 - A5)
        pins[pin] = POT;
        inputPins[numInputPins] = pin;
        numInputPins++;

        pinMode(pin, INPUT);
      }

      else if (pin <= 25){ // Button (A0 - A5)
        pins[pin] = BTN;
        inputPins[numInputPins] = pin;
        numInputPins++;

        pinMode(pin - 6, INPUT);
      }
    
    }

    else {
      if (pins[pin] == LED){
        if ((command & 0x02) >> 1 == 1){
          digitalWrite(CleanPin(pin), HIGH);
        }
        else{
          digitalWrite(CleanPin(pin), LOW);
        }
      }
      else if (pins[pin] == IR){
        int val = (command & 0x02) >> 1;

        if (command & 1 == 1){ //data contiues
          byte data;
          int byteNum = 0;
          do
          {
              Serial.readBytes(&data, 1);
              val |= (data & 0xFE) << ((7 * byteNum));
              byteNum++;
          }
          while ((data & 1) == 1);
        }
        IrSender.sendNEC(IrAddress, val, 0);
      }
    }
  }
}


void Reset(){
  numInputPins = 0;
  for (int i = 0; i < 26; i++){
    pins[i] = NONE;
  }
}

int CleanPin(int pin){
  if (pin <= 19){ // D0 - D13, A0 - A5
      return pin;
    }
    else{ // DA0 - DA5
      return pin - 6;
    }
}
