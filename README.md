BBB_Gforth_gpio
===============

GPIO access using mmap in Gforth for Beagle Bone Black Rev C

This code is a gforth gpio solution.  It will use c code writen in the gforth file 
as a shared library that will access gpio via mmap funciton in c.  The GPIO library functions  
will allow setup and access to any gpio pin but note these pins are not all brought out to P8 and P9 header
and most pins are used by other thing on the Beagle Bone Black.  The device tree stuff will need to be 
externaly handled and a good way to do this is to use bonescript pinMode function as you can setup pullup 
and slew rate settings. Note the gpio code will still work on pins that do not conflict with hardware or 
other device tree settings directly with out using any device tree stuff!  

After researching this issue for some time i came to the conclusion that i can do this GPIO stuff with
two steps.  The first step is the device tree setup and i decided this to be done externaly.
The second step is this code will use mmap as the main method to set up the beagle bone black for 
GPIO access.  

The words in this library allow input and output of any of the gpio banks and pins.  However realize 
these gpios are not all brought out to P8 and P9 headers and some are connected to internal devices. 
This means you need to look at the Beagle Bone Black schematics to figure out what P9 or P8 pins connect to 
in order to use the gforth words to connect to the real world pins.  An example of this is P9_12 pin is
GPIO_28 GPIO bank 1.  So the setup to use this pin in my code would be as follows:

1 0x10000000 bbbiosetup 

This now lets you use the other words to read that pin or write data to the pin as follows:

bbbiooutput<br> 
bbbioset
bbbioclear
bbbioinput 
bbbioread .  \ this would produce 0 or 268435456 ( this is decimal for 0x10000000)

So you can see how easy it is to do things to the pin.  After you should close the pin down as follows:
bbbiocleanup
Now what ever settings were in the GPIO_DATAOUT and GPIO_OE registers at the start of this code should 
now be restored!
This method of setting bank and gpio pin number allows more then one pin from a bank to be played with at
a time so it give good flexability.  The speeds i can achieve with this method is around 500 khz  so it is
much better then using linux command line or bonescript itself as they are around 4 khz.  

Note i have played with a toggleing idea in c that the gforth code could used but i found the highest speed
i could get pins to toggle were around 1 mhz so i felt it was not really need to add to this library as
500 khz is plenty for what i will be using this for. 