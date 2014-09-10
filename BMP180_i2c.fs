\ This Gforth code reads BMP180 pressure temperature sensor via i2c on BBB rev c hardware 
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

\  

require BBB_I2C_lib.fs

true    constant datasheet   \ set to true to test with data sheet data false for normal operation

22      constant EEprom      \ the size of calibration data eeprom on bmp180 device in bytes
0x77    constant BMP180ADDR  \ I2C address of BMP180 device
1000000 constant BMP180FREQ  \ Data sheet says 3.4 MHz not sure how to use this in linux yet!
0xF6    constant CMD_READ_VALUE 
0xAA    constant CMD_READ_CALIBRATION 
0       constant OVERSAMPLING_ULTRA_LOW_POWER \ these are the constants for in the below code!
1	constant OVERSAMPLING_STANDARD
2	constant OVERSAMPLING_HIGH_RESOLUTION 
3	constant OVERSAMPLING_ULTRA_HIGH_RESOLUTION
1       constant i2cbus

0 value i2c-handle
create buff 0 c, 0 c, 0 c,  \ make room for 3 bytes 

struct
    char% field data%
end-struct raw-cal%
create eeprom-data
eeprom-data raw-cal% %size EEprom * dup allot erase

: eeprom@ ( nindex -- cdata% )  \ simply retrieve byte data for processing
    raw-cal% %size * eeprom-data + c@ ;

struct
    cell% field ac1  \ signed
    cell% field ac2  \ signed
    cell% field ac3  \ signed
    cell% field ac4  \ unsigned
    cell% field ac5  \ unsigned
    cell% field ac6  \ unsigned
    cell% field b1   \ signed 
    cell% field b2   \ signed
    cell% field mb   \ signed
    cell% field mc   \ signed
    cell% field md   \ signed
end-struct cal%

create cal-para
cal% %allot cal% %size erase

: i2ctest ( n -- ) \ if n is zero then throw 
    0 = throw ;

i2cbus BMP180ADDR bbbi2copen dup i2ctest to i2c-handle
i2c-handle CMD_READ_CALIBRATION bbbi2cwrite-b throw
i2c-handle eeprom-data EEprom bbbi2cread i2ctest

: getsigned-calvalue ( nindex -- nsigned-cal ) \ retreaves the signed calibation value based on the eeprom-data index
    dup 1 + eeprom@ swap eeprom@ 0x100 * + 0x10000 * 0x10000 / ;  \ note this will only work on 32 bit BBB processor 

: getunsigned-calvalue ( nindex -- nunsigned-cal ) \ retreaves the unsigned calibration value based on the eeprom-data index
    dup 1 + eeprom@ swap eeprom@ 0x100 *  + ; 

0  getsigned-calvalue   cal-para ac1 !
2  getsigned-calvalue   cal-para ac2 !
4  getsigned-calvalue   cal-para ac3 !
6  getunsigned-calvalue cal-para ac4 !
8  getunsigned-calvalue cal-para ac5 !
10 getunsigned-calvalue cal-para ac6 !
12 getsigned-calvalue   cal-para b1 !
14 getsigned-calvalue   cal-para b2 !
16 getsigned-calvalue   cal-para mb !
18 getsigned-calvalue   cal-para mc !
20 getsigned-calvalue   cal-para md !
\ now all the calibration data is retreaved and put in variables

\ test with original data
datasheet [if]
408 cal-para ac1 !
-72 cal-para ac2 !
-14383 cal-para ac3 !
32741 cal-para ac4 !
32757 cal-para ac5 !
23153 cal-para ac6 !
6190 cal-para b1 !
4 cal-para b2 !
-32768 cal-para mb !
-8711 cal-para mc !
2868 cal-para md !
[then]
    
\ read uncompensated temperature register
0xf4 buff c!
0x2e buff 1 + c!
i2c-handle buff 2 bbbi2cwrite i2ctest
6 ms
i2c-handle 0xf6 bbbi2cwrite-b throw 
i2c-handle buff 2 bbbi2cread i2ctest
buff c@ 8 lshift buff 1 + c@ or value ut

\ test with data sheet data
datasheet [if]
27898 to ut
[then]
    
\ read uncompensated pressure register
0xf4 buff c!
0x34 OVERSAMPLING_ULTRA_LOW_POWER 6 lshift + buff 1 + c!
i2c-handle buff 2 bbbi2cwrite i2ctest
OVERSAMPLING_ULTRA_LOW_POWER 1 + 10 * ms
i2c-handle 0xf6 bbbi2cwrite-b throw
i2c-handle buff 3 bbbi2cread i2ctest
buff c@ 16 lshift buff 1 + c@ 8 lshift or buff 2 + c@ or
8 OVERSAMPLING_ULTRA_LOW_POWER - rshift value up

\ test with data sheet data
datasheet [if]
23843 to up 
[then]

\ done with i2c communications
i2c-handle bbbi2cclose throw

\ compensate temperature
ut cal-para ac6 @ - s>f
cal-para ac5 @ s>f f*
32768e f/ create x1 f,
cal-para mc @ s>f 2048e f*
x1 f@ cal-para md @ s>f f+ f/ create x2 f,
x1 f@ x2 f@ f+ create b5 f,
b5 f@ 8e f+ 16e f/ create t f,
t f@ 10e f/ create deg f,

\ compensate pressure
b5 f@ 4000e f- create b6 f,
b6 f@ fdup f* 4096e f/ cal-para b2 @ s>f f* 2048e f/ x1 f!
cal-para ac2 @ s>f b6 f@ f* 2048e f/ x2 f!
x2 f@ x1 f@ f+ create x3 f,
cal-para ac1 @ s>f 4e f* x3 f@ f+ f>s OVERSAMPLING_ULTRA_LOW_POWER lshift 2 + s>f 4e f/ create b3 f,
cal-para ac3 @ s>f b6 f@ f* 8192e f/ x1 f!
cal-para b1 @ s>f b6 f@ fdup f* 4096e f/ f* 65536e f/ x2 f!
x1 f@ x2 f@ f+ 2e f+ 4e f/ x3 f!
cal-para ac4 @ s>f x3 f@ 32768e f+ f* 32768e f/ create b4 f,
up s>f b3 f@ f- 50000 OVERSAMPLING_ULTRA_LOW_POWER rshift s>f f* create b7 f,
b7 f@ 2e f* b4 f@ f/ create pa f,

create floatoutputbuffer
10 allot 
: fto$ ( f: r -- caddr u ) \ convert r from float stack to string
        floatoutputbuffer 4 1 0 f>buf-rdp floatoutputbuffer 10 ;

." Temperature is " deg f@ fto$ type ."  C" cr

." Pressure is " pa f@ f>s . ."  pa" cr 
bye

