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
\c #define IOGOOD            0xf
\c #define IOBAD             -1

\c static int mem_fd = 0;
\c unsigned int areg = 0;
\c char *gpio_map;
\c volatile unsigned *gpio;
\c int bits;
\c unsigned int out_en = 0;
\c unsigned int data_out = 0;
\c int gpio_setup = 0;

\c int gpiosetup(int gpio_bank, int gpio_bits ){
\c unsigned int bank = 0;
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
\c     return(IOBAD); }
    
\c /* open /dev/mem */
\c if ((mem_fd = open("/dev/mem", O_RDWR|O_SYNC) ) < 0) {
\c     printf("can't open /dev/mem \n");
\c     return(IOBAD); }

\c /* mmap GPIO */
\c gpio_map = (char *)mmap( 0, GPIO_SIZE, PROT_READ|PROT_WRITE,
\c     MAP_SHARED, mem_fd, bank );

\c /* mmap fail test */    
\c if (gpio_map == MAP_FAILED) {
\c     printf("mmap error %d\n", (int)gpio_map);
\c     return(IOBAD); }

\c bits = gpio_bits ;
    
\c // Always use the volatile pointer!
\c gpio = (volatile unsigned *)gpio_map;

\c /* save GPIO_OE */
\c out_en = *(gpio + GPIO_OE);

\c /* save GPIO_DATAOUT */    
\c data_out = *(gpio + GPIO_DATAOUT);
    
\c gpio_setup = IOGOOD;
\c return 0 ;}

\c int gpiocleanup(void) {
\c int errors = 0;
\c if(gpio_setup == IOGOOD){
\c *(gpio + GPIO_OE) = out_en; // restore GPIO_OE
\c *(gpio + GPIO_DATAOUT) = data_out; // restore GPIO_DATAOUT
\c errors = close(mem_fd);
\c mem_fd = 0;
\c gpio_map = 0;
\c areg = 0;
\c gpio = 0;
\c bits = 0;
\c out_en = 0;
\c data_out = 0;
\c gpio_setup = 0;
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

\c int gpioread(void) {
\c if(gpio_setup == IOGOOD){
\c return (*(gpio + GPIO_DATAIN) & bits);
\c } else {
\c return (0); } }

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
c-function bbbioread     gpioread          -- n
c-function bbbioset      gpioset           -- void
c-function bbbioclear    gpioclear         -- void

end-c-library

\ bbbiosetup ( nbank ngpiobits -- nflag )
\ This will set up the ability to access gpio nbank and ngpiobits 
\ nbank is 0,1,2,3 any other number returns nflag of true (-1)
\ ngpiobits is a 32 bit number one bit per gpio bit 
\ nflag is 0 for gpio operations all good any other value is an error
\ This code does not switch any modes or do any operations on registers 
\ but now other functions can be used to access the gpio functions.
\ Note this is bank and gpiobits are not Beagle Bone Black P8 or P9 pins 
\ so to find out what P8 or P9 pins are maped to gpio bits you need the 
\ Beagle Bone Black schematics and the AM335x Technical Reference Manual

\ bbbiocleanup ( -- nflag )
\ This will release the mmap memory that is accessed in bbbiosetup and restore 
\ the values in GPIO_OE and GPIO_DATAOUT registers at bbbiosetup entry values
\ thus restoring the output settings and the current output levels.
\ Note if bbbiosetup was not called before this function then nflag is true (-1)
\ and nothing happens to chip registers at all.
\ This word should be used after you are done with gpio stuff!

\ bbbioinput ( -- ) 
\ This will set the previously registered bank and gpio bits to input mode.
\ Use this before a bbbioread function to read from the gpio bits.

\ bbbiooutput ( -- ) 
\ This will set the previously registered bank and gpio bits to output mode.
\ Use this before a bbbioset or bbbioclear words for output functions.

\ bbbioread  ( -- nvalue )
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

\ 

