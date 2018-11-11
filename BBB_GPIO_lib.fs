\ This Gforth code gives GPIO functionality to BBB rev c hardware in Gforth
\    Copyright (C) 2014  Philip K. Smith

\    This program is free software: you can redistribute it and/or modify
\    it under the terms of the GNU General Public License as published by
\    the Free Software Foundation, either version 3 of the License, or
\    (at your option) any later version.

\    This program is distributed in the hope that it will be useful,
\    but WITHOUT ANY WARRANTY; without even the implied warranty of
\    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\    GNU General Public License for more details.

\    You should have received a copy of the GNU General Public License
\    along with this program.  If not, see <http://www.gnu.org/licenses/>.

\  This software will give access to several GPIO pins but will not take care of Device Tree settings at all
\  The device tree should be set up befor using this software.
\  Note it is possible to access the GPIO stuff without setting device tree stuff but
\  be aware that doing so may cause confusion in linux
\  This software works with Debian Wheezy and may work on other versions.


c-library myBBBGPIO
\c #include <stdio.h>
\c #include <unistd.h>
\c #include <sys/mman.h>
\c #include <stdlib.h>
\c #include <sys/stat.h>
\c #include <fcntl.h>
\c #include <errno.h>

\c #define GPIO0_BASE 0x44E07000
\c #define GPIO1_BASE 0x4804C000
\c #define GPIO2_BASE 0x481AC000
\c #define GPIO3_BASE 0x481AE000

\c #define GPIO_SIZE  0x00000FFF

\c // OE: 0 is output, 1 is input
\c #define GPIO_OE           (0x134 /4)
\c #define GPIO_DATAIN       (0x138 /4)
\c #define GPIO_DATAOUT      (0x13c /4)
\c #define GPIO_CLEARDATAOUT (0x190 /4)
\c #define GPIO_SETDATAOUT   (0x194 /4)
\c #define IOGOOD            0
\c #define IOBAD             -1

\c volatile int mem_fd = 0;
\c volatile unsigned int areg = 0;
\c volatile char *gpio_map;
\c volatile unsigned *gpio;
\c volatile int bits;
\c volatile int gpio_setup = IOBAD;

\c int gpiosetup(int gpio_bank, int gpio_bits ){
\c unsigned int bank = 0;
\c if (gpio_setup == IOBAD) {
\c switch(gpio_bank){
\c case 0 :
\c     bank = GPIO0_BASE;
\c     break;
\c case 1 :
\c     bank = GPIO1_BASE;
\c     break;
\c case 2 :
\c     bank = GPIO2_BASE;
\c     break;
\c case 3 :
\c     bank = GPIO3_BASE;
\c     break;
\c default :
\c     return(gpio_setup); }

\c /* open /dev/mem */
\c if ((mem_fd = open("/dev/mem", O_RDWR|O_SYNC) ) < 0) {
\c     fprintf(stderr,"can't open /dev/mem error: %s\n",strerror(errno));
\c     return(gpio_setup); }

\c /* mmap GPIO */
\c gpio_map = (char *)mmap( 0, GPIO_SIZE, PROT_READ|PROT_WRITE,
\c     MAP_SHARED, mem_fd, bank );

\c /* mmap fail test */
\c if (gpio_map == MAP_FAILED) {
\c     fprintf(stderr,"mmap error: %d more: %s\n", (int)gpio_map,strerror(errno));
\c     return(gpio_setup); }

\c bits = gpio_bits ;

\c // Always use the volatile pointer!
\c gpio = (volatile unsigned *)gpio_map;

\c gpio_setup = IOGOOD;
\c return (gpio_setup) ; }
\c return (IOBAD); }

\c int gpiocleanup(void) {
\c int errors = gpio_setup;
\c if(gpio_setup == IOGOOD){
\c munmap(gpio,GPIO_SIZE);
\c errors = close(mem_fd);
\c mem_fd = 0;
\c gpio_map = 0;
\c areg = 0;
\c gpio = 0;
\c bits = 0;
\c gpio_setup = IOBAD;
\c } else {
\c errors = IOBAD;
\c }
\c return (errors); }

\c void gpioinput(void) {
\c if(gpio_setup == IOGOOD){
\c areg = *(gpio + GPIO_OE);
\c areg = areg | bits;
\c *(gpio + GPIO_OE) = areg;} }

\c void gpiooutput(void) {
\c if(gpio_setup == IOGOOD){
\c areg = *(gpio + GPIO_OE);
\c areg = areg & (~bits);
\c *(gpio + GPIO_OE) = areg; } }

\c int gpioread(unsigned *values) {
\c if(gpio_setup == IOGOOD){
\c *values = (*(gpio + GPIO_DATAIN) & bits);
\c return (IOGOOD);
\c } else {
\c return (IOBAD); } }

\c int gpioreadf(void) { return (*(gpio + GPIO_DATAIN) & bits); }

\c void  gpioset(void) {
\c if(gpio_setup == IOGOOD){
\c /* this method reads current output bits and ors new bits to them */
\c // *(gpio + GPIO_DATAOUT) = *(gpio + GPIO_DATAOUT) | bits; }
\c /* this method simply uses the setdataout register */
\c *(gpio + GPIO_SETDATAOUT) = bits; } }

\c void gpioclear(void) {
\c if(gpio_setup == IOGOOD){
\c /* this method reads current output bits and ors complimented new bits to them */
\c // *(gpio + GPIO_DATAOUT) = *(gpio + GPIO_DATAOUT) & (~bits); }
\c /* this method simply uses the cleardataout register */
\c *(gpio + GPIO_CLEARDATAOUT) = bits; } }

c-function bbbiosetup    gpiosetup     n n -- n
c-function bbbiocleanup  gpiocleanup       -- n
c-function bbbioinput    gpioinput         -- void
c-function bbbiooutput   gpiooutput        -- void
c-function bbbioread     gpioread        a -- n
c-function bbbioset      gpioset           -- void
c-function bbbioclear    gpioclear         -- void
c-function bbbioreadf    gpioreadf         -- n

end-c-library
\ NOTE: Do not connect 5V logic level signals to these pins or the board will be damaged.
\ NOTE: DO NOT APPLY VOLTAGE TO ANY I/O PIN WHEN POWER IS NOT SUPPLIED TO THE BOARD.
\ IT WILL DAMAGE THE PROCESSOR AND VOID THE WARRANTY.
\ NO PINS ARE TO BE DRIVEN UNTIL AFTER THE SYS_RESET LINE GOES HIGH

\ These gforth words have the ability to damage your Beagle Bone Black.
\ With great power comes great responsibility!  Understand the above warnings from
\ the Beagle Bone Black reference materials!
\ Please ensure you understand how to use this code before hooking anything up
\ to your BBB.  I have been able to toggle pins on and off at 500 khz with this code.
\ I have not needed faster then this speed.  It is possible with extending this
\ code with a toggling function. Testing with this idea i was able to reach around
\ 1 mhz.

\ bbbiosetup ( nbank ngpiobits -- nflag )
\ This will set up the ability to access gpio nbank and ngpiobits
\ nbank is 0,1,2,3 any other number returns nflag of true (-1)
\ ngpiobits is a 32 bit number one bit per gpio bit
\ nflag is 0 for gpio operations all good any other value is an error
\ ngpiobits allows many bits to be turned on at once or use it for just one bit.
\ This code does not switch any modes or do any operations on registers
\ but now other functions can be used to access the gpio functions.
\ Note this is bank and gpiobits are not Beagle Bone Black P8 or P9 pins
\ so to find out what P8 or P9 pins are maped to gpio bits you need the
\ Beagle Bone Black schematics and the AM335x Technical Reference Manual

\ bbbiocleanup ( -- nflag )
\ This will release the mmap memory that is accessed in bbbiosetup and restore
\ the values in GPIO_OE and GPIO_DATAOUT registers at bbbiosetup entry values
\ thus restoring the output settings and the original output levels.
\ Note if bbbiosetup was not called before this function then nflag is true (-1)
\ and nothing happens to chip registers at all.
\ This word should be used after you are done with gpio stuff!

\ bbbioinput ( -- )
\ This will set the previously registered bank and gpio bits to input mode.
\ Use this before a bbbioread function to read from the gpio bits.

\ bbbiooutput ( -- )
\ This will set the previously registered bank and gpio bits to output mode.
\ Use this before a bbbioset or bbbioclear words for output functions.

\ bbbioread  ( addrvalue -- nflag )
\ addrvalue is an address for a variable that will be used to return the value
\ nflag is false if the read value in addrvalue is valid.  It is true if data is not valid.
\ This will return the currently read value from previously registed bank and gpio bits.
\ You can read as many times as you want as long as before the first read you use
\ the word bbbioinput to set that mode.
\ No error will be returned if you are not in reading mode and the value
\ returned may not be the correct gpio bit value.
\ Note if you are not in reading  mode nothing will happen to the hardware
\ so no damage can happen with this command.
\ Note if you did not use bbbiosetup before this word then nvalue will always be 0.
\ I haved noticed you can set the gpio bits to output and then use this bbbioread word
\ to read the bits correctly as you change the output values with bbbioset and bbbioclear
\ but this is dependent on the internal gate methods used to put data into the
\ GPIO_DATAIN register of the SOC.  When doing this it seems like the pins on P8 or P9
\ do in fact change with the output and the read values changes with them!

\ bbbioreadf ( -- nvalue )
\ This will read the internal read register reguardless of the mode you are in
\ and return the 32 bit value of that register.  This read is not destructive
\ so nothing will happen if you are not in input mode but it may not have
\ the correct data you want if you are not in input mode.
\ The this word is faster the bbbioread because you do not need to pass an address
\ to the word.

\ bbbioset ( -- )
\ This will turn on the bank and gpio bit that was previously set with bbbiosetup
\ Remember to use bbbiooutput before this word is used.
\ You can use this word as many times as you want after setting output mode but
\ realize you will not see a change after you set a bit unless you clear it
\ then set again.
\ Note if you have not used bbbiooutput before this word nothing will happen
\ to output.
\ Note if you have not used bbbiosetup before this word nothing will happen to
\ the registers.

\ bbbioclear ( -- )
\ This will turn off the bank and gpio bit that was previously set with bbbiosetup
\ Remember to use bbbiooutput before this word is used.
\ You can use this word as many times as you want after setting output mode but
\ realize you will not see any change after you clear a bit unless you set the bit again
\ then clear it.
\ Note if you have not used bbbiooutput before this word nothing will happen to output.
\ Note if you have not used bbbiosetup before this word nothing will happen to the
\ registers.

\ example #1
\ 1 constant bank1
\ 0x10000000 constant gpio28
\ bank1 gpio28 bbbiosetup throw
\ bbbiooutput
\ bbbioclear
\ bbbioset
\ bbbioclear
\ bbbiocleanup

\ The above example will put one pulse of low to high to low out on P9_12 header pin

\ example #2
\ : starttoggle ( -- ) 1 0x10000000 bbbiosetup throw bbbiooutput bbbioclear ;
\ : toggle ( -- ) bbbioset bbbioclear ;
\ : ntoggle ( n -- ) 0 ?do toggle loop ;
\ : finishtoggle ( -- )  bbbiocleanup throw ;
\ starttoggle 10 ntoggle finishtoggle

\ This example will make a pulse train of 10 low to high pulse at P9_12 header pin

\ example #3
\ 1 constant bank1
\ 0x10000000 constant gpio28
\ variable pinvalue
\ bank1 gpio28 bbbiosetup throw
\ bbbioinput
\ pinvalue bbbioread throw pinvalue @ gpio28 = cr ." P9_12 " [if] ." is High" [else] ." is Low" [then]
\ bbbiocleanup throw

\ This example will read P9_12 header pin

\ example #4
\ 1 constant bank1
\ 0x10000000 constant gpio28
\ bank1 gpio28 bbbiosetup throw
\ bbbioinput
\ bbbioreadf gpio28 = cr ." P9_12 " [if] ." is High" [else] ." is Low" [then]
\ bbbiocleanup throw
\
\ This example will read P9_12 header pin.  Testing has shown this to be twice as fast as bbbioread.
\ The reason for the increased speed is because this word does not need to pass anything to the read
\ function.  This demistrates passing things back and forth from Gforth to C is the main performace
\ barrier here not the C code running or the Gforth code running as they are fast!
