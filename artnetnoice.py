import time
import random
from stupidArtnet import StupidArtnet

# Target IP (e.g., your lighting console, node, or 127.0.0.1 for local software)
target_ip = '127.0.0.1'
universe = 0

# Initialize Artnet object
artnet = StupidArtnet(target_ip, universe)

# Create a DMX array of 512 channels, all initialized to 0
dmx_data = [0] * 512

i = 0
dmx_data[0] = 13
artnet.send(dmx_data)
time.sleep(.03)
try:
    while True:
        # Assign random RGB values to the first three DMX channels
        #print("Value of DMX channel 0: " + str(i))
        #dmx_data[0] = i
        dmx_data[0] = int(input("Value of DMX channel 0: "))
        # Send data to universe
        
        if (i == 31):
            pass
        i += 1
        artnet.send(dmx_data)
        # Art-Net typically requires 30-40 Hz refresh rate
        time.sleep(0.03)
        #time.sleep(.5)
        #time.sleep(1.5) 

except KeyboardInterrupt:
    # Safely close the socket when stopping
    artnet.stop()
