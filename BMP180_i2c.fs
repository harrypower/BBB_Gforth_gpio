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

22      constant EEprom      \ the size of calibration data eeprom on bmp180 device in bytes
0x77    constant BMP180ADDR  \ I2C address of BMP180 device
1000000 constant BMP180FREQ  \ Data sheet says 3.4 MHz not sure how to use this in linux yet!
0xF6    constant CMD_READ_VALUE 
0xAA    constant CMD_READ_CALIBRATION 
0       constant OVERSAMPLING_ULTRA_LOW_POWER \ these are the constants used in the oversample variable in the below code!
1	constant OVERSAMPLING_STANDARD
2	constant OVERSAMPLING_HIGH_RESOLUTION 
3	constant OVERSAMPLING_ULTRA_HIGH_RESOLUTION
1       constant i2cbus

0 value i2c-handle

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