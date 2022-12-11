# HMM
 Hexa's Memory Mod for LogicWorld 0.90.3. Version 0.0.2

## Memory Basic

### Word D Latches and Word Relays

D Latches and Relays for 1 byte, 2 byte, 4 byte, and 8 byte data.

## Memory Modules
 
### Memory8bit

A simple 8 bit memory component that resembles a D-Latch. Has a 16 bit address for a total of 65,536 bytes.

### HexRom8bit

A read only memory component that allows hexadecimal to be entered just like a Label. 16 bit address allows for up to 65,536 bytes. The least significant bit is on the bottom left and bottom right for the address and output respectively.
The address points to the 2nth character in the text. That character and the one after is converted into a byte.

### AsmRom8bit

A read only memory component with built in assembler. Instructions with corresponding machine code is specified, along with some configuration parameters, and allow basic assembly language to be converted into different architectures. For more information, refer to the AsmROM manual.

## Display

### PixelDisplay

A resizable 24 bit color display. The pins on the back from bottom row to top are x,y,r,g,b,control. The control pins from edge to center: clock, floodfill. The display can be resized from a minimum of 3x2 up to 16x16. With 16x16 pixels each square, thats 48x32 to 256x256. Input positions outside current size are ignored. Screen origin(0,0) is on the bottom left. It has a refresh rate of 10 fps.