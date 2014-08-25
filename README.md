BBB_Gforth_gpio
===============

GPIO access using mmap in Gforth for Beagle Bone Black Rev C

This code will be in the end a gforth gpio solution.  It will use c code writen in the gforth code 
as a shared library that will access gpio via mmap funciton in c.  The code will only work if the 
P8 or P9 headers and the gpio pins are set up via an external device tree file.  This device tree
setup process will not be part of this code at first but maybe later this will be included.

After researching this issue for some time i came to the conclusion that i can do this GPIO stuff with
two steps.  The first step is the device tree setup and i decided this to be done externaly at first.
The second step is this code will use mmap as the main method to set up the beagle bone black for 
GPIO access.  To this end i have found an example to start with that i then will use as a basic 
framework in gforth to get the mmap stuff working.  This example was found here: http://stackoverflow.com/questions/13124271/driving-beaglebone-gpio-through-dev-mem
It does what i want and is simple to start with.  

To this end currently the example code in c is included in this repo.  The code blinks all four user
leds.  Note that this code in order to see the leds blinking properly will need to disable the 
current heartbeat of the leds.  This simply can be done on the bone with connecting to your bone
in a browser at http://192.168.0.115/Support/BoneScript/#timers ( change the address to match your BBB address).
This page has three pieces of code to turn the leds on and off and restore there function.  
If you use this to turn the leds off then run the example code you will see the leds all blink slowly.
  