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
\  This software is setup assuming the use of Debian Wheezy and may work on other versions.


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
\c // #define GPIO_OE (0x134 / 4)
\c #define GPIO_OE 0x4d
\c // #define GPIO_DATAIN (0x138 / 4)
\c #define GPIO_DATAIN 0x4e
\c // #define GPIO_DATAOUT (0x13c / 4)
\c #define GPIO_DATAOUT 0x4f

\c #define GPIO1_28 (1<<28)
\c #define GPIO0_7  (1<<7)

\c int mem_fd;
\c char *gpio_mem, *gpio_map;
\c volatile unsigned *gpio;

\c static int io_setup(void){
\c // Enable all GPIO banks
\c // Without this, access to deactivated banks (i.e. those with no clock source set up) will (logically) fail with SIGBUS
\c // Idea taken from https://groups.google.com/forum/#!msg/beagleboard/OYFp4EXawiI/Mq6s3sg14HoJ
\c system("echo 5 > /sys/class/gpio/export");
\c system("echo 65 > /sys/class/gpio/export");
\c system("echo 105 > /sys/class/gpio/export");

\c /* open /dev/mem */
\c if ((mem_fd = open("/dev/mem", O_RDWR|O_SYNC) ) < 0) {
\c     printf("can't open /dev/mem \n");
\c     exit (-1); }

\c /* mmap GPIO */
\c gpio_map = (char *)mmap( 0, GPIO_SIZE, PROT_READ|PROT_WRITE,
\c     MAP_SHARED, mem_fd, GPIO0_BASE );

\c if (gpio_map == MAP_FAILED) {
\c     printf("mmap error %d\n", (int)gpio_map);
\c     exit (-1); }
    
\c // Always use the volatile pointer!
\c gpio = (volatile unsigned *)gpio_map;

\c // Get direction control register contents
\c unsigned int creg = *(gpio + GPIO_OE);

\c // set output
\ \c creg = creg & (~GPIO1_28);
\c creg = creg & (~GPIO0_7);
\c *(gpio + GPIO_OE) = creg;

\c int i;
\c int test;
    
\c for( i = 0; i < 100000 ; i++ ){
\ \c     *(gpio + GPIO_DATAOUT) = *(gpio + GPIO_DATAOUT) | GPIO1_28;
\c     *(gpio + GPIO_DATAOUT) = *(gpio + GPIO_DATAOUT) | GPIO0_7;
\c     usleep(1);
\ \c     *(gpio + GPIO_DATAOUT) = *(gpio + GPIO_DATAOUT) & (~GPIO1_28);
\c     *(gpio + GPIO_DATAOUT) = *(gpio + GPIO_DATAOUT) & (~GPIO0_7);
\c     usleep(1); }

\c return ( 0 ) ;
\c }
\c 
\c static int gpiosetup(void) { return 0; }
\c int gpiocleanup(void) { return 0; }
\ \c int gpiopullup(int gb, int gp) { return 0; }
\ \c int gpiopulldown(int gb, int gp) { return 0; }
\ \c int gpiopulloff(int gb, int gp) { return 0; }
\c int gpioinput(int gb, int gp) { return 0; }
\c int gpiooutput(int gb, int gp) { return 0; }
\c static int gpioread(int *error, int gb, int gp) { return 0; }
\c int gpioset(int gb, int gp) { return 0; }
\c int gpioclear(int gb, int gp) { return 0; }

c-function GPIO-setup   io_setup          -- n 

c-function bgiosetup    gpiosetup         -- n
c-function bgiocleanup  gpiocleanup       -- n
\ c-function bgiopullup   gpiopullup    n n -- n
\ c-function bgiopulldown gpiopulldown  n n -- n
\ c-function bgiopulloff  gpiopulloff   n n -- n
c-function bgioinput    gpioinput     n n -- n
c-function bgiooutput   gpiooutput    n n -- n
c-function bgioread     gpioread    a n n -- n
c-function bgioset      gpioset       n n -- n
c-function bgioclear    gpioclear     n n -- n


end-c-library
