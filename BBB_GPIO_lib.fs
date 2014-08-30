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
\c #define GPIO_OE           (0x134 /4)
\c #define GPIO_DATAIN       (0x138 /4)
\c #define GPIO_DATAOUT      (0x13c /4)

\c #define GPIO1_28 (1<<28)

\c static int mem_fd = 0;
\c unsigned int areg = 0;
\c char *gpio_map;
\c volatile unsigned *gpio;

\c int gpiosetup(int gpio_bank){
\c /* open /dev/mem */
\c if ((mem_fd = open("/dev/mem", O_RDWR|O_SYNC) ) < 0) {
\c     printf("can't open /dev/mem \n");
\c     exit (-1); }

\c /* mmap GPIO */
\c gpio_map = (char *)mmap( 0, GPIO_SIZE, PROT_READ|PROT_WRITE,
\c     MAP_SHARED, mem_fd, GPIO1_BASE );

\c if (gpio_map == MAP_FAILED) {
\c     printf("mmap error %d\n", (int)gpio_map);
\c     exit (-1); }

\c // Always use the volatile pointer!
\c gpio = (volatile unsigned *)gpio_map;

\c // Get direction control register contents
\c areg = *(gpio + GPIO_OE);

\c // set output
\c areg = areg & (~GPIO1_28);
\c *(gpio + GPIO_OE) = areg;

\c int i;
\c for( i = 0; i < 10000 ; i++ ){
\c     *(gpio + GPIO_DATAOUT) = *(gpio + GPIO_DATAOUT) | GPIO1_28;
\c     *(gpio + GPIO_DATAOUT) = *(gpio + GPIO_DATAOUT) & (~GPIO1_28);
\c } return 0 ;}

\c void gpiocleanup(void) {
\c close(mem_fd);
\c mem_fd = 0;
\c gpio_map = 0;
\c areg = 0;
\c gpio = 0; }

\c int gpioinput(int gpio_pins) {
\c areg = *(gpio + GPIO_OE);
\c areg = areg | gpio_pins;
\c *(gpio + GPIO_OE) = areg;
\c return 0; }

\c int gpiooutput(int gpio_pins) {
\c areg = *(gpio + GPIO_OE);
\c areg = areg & (~gpio_pins);
\c *(gpio + GPIO_OE) = areg;
\c return 0 ; }

\c int gpioread(int *error, int gpio_pins) { return 0; }

\c void  gpioset(int gpio_pins) {
\c *(gpio + GPIO_DATAOUT) = *(gpio + GPIO_DATAOUT) | gpio_pins; }
\ \c return 0; }

\c void gpioclear(int gpio_pins) {
\c *(gpio + GPIO_DATAOUT) = *(gpio + GPIO_DATAOUT) & (~gpio_pins); }
\ \c return 0; }

c-function bbbiosetup    gpiosetup       n -- n
c-function bbbiocleanup  gpiocleanup       -- void
c-function bbbioinput    gpioinput       n -- n
c-function bbbiooutput   gpiooutput      n -- n
c-function bbbioread     gpioread     a  n -- n
c-function bbbioset      gpioset         n -- void
c-function bbbioclear    gpioclear       n -- void

end-c-library

