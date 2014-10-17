#! /usr/local/bin/gforth
\ the above line works on 0.7.3 gforth and up
\ #! /usr/bin/gforth
\ version 0.7.0 has the /local removed from the path to work

\ This Gforth code reads HTU21D temperature humidity sensor via i2c on BBB rev c hardware 
\ This code reads i2c-1 but this should be mapped to i2c-2 device on p9 header.
\ The method of reading the sensor is 'Hold Master' and this means this sensor
\ will hold the i2c data lines until a reading is done!
\ 
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

variable i2c_handle
create abuffer 3 allot     \ for transfering in and out of device

0x40 constant htu21d_addr  \ i2c address of sensor
1    constant i2c_addr
0xe3 constant temp_read
0xe5 constant humd_read

: setup-htu21d ( -- nflag ) \ nflag is true if handle invalid false if handle is valid
    i2c_addr htu21d_addr bbbi2copen dup i2c_handle !
    true =
 ;

: read-temp ( -- temp nflag ) \ nflag is true if some reading error happended false if temp is valid
    ( f: -- temp ) \ the floating stack will also contain the temperature value 
    try  \ the temp value is temperature but you need to divide by 10 to get the correct value!
	i2c_handle @ temp_read bbbi2cwrite-b throw
	i2c_handle @ abuffer 3 bbbi2cread 3 <> throw
	abuffer c@ 8 lshift
	abuffer 1 + c@
	%11111100 and +
	s>d d>f
	65536e f/
	175.72e f*
	-46.85e fswap f+ fdup 10e f* 
	f>d d>s
	false
    restore dup if 0 swap then 
    endtry ;

: read-humd ( -- humd nflag )  \ nflag is true is some reading error happeneded false if humd is valid
    ( f: -- humd ) \ the floating stack will also contain the humidity value
    try   \ the humd value is humidity but you need to divide by 10 to get the correct value!
	i2c_handle @ humd_read bbbi2cwrite-b throw 
	i2c_handle @ abuffer 3 bbbi2cread 3 <> throw 
	abuffer c@ 8 lshift
	abuffer 1 + c@
	%11111100 and +  
	s>d d>f
	65536e f/
	125e f*
	-6e fswap f+ fdup 10e f* 
	f>d d>s
	false 
    restore dup if 0 swap then 
    endtry ;

: read-THcorrected ( -- temp humd nflag ) \ nflag is true if readings are invalid and false if valid
    \ temp is temperature but to get the correct value you must divide by 10
    \ humd is humidity but to get the correct value you  must divide by 10
    try
	read-temp throw drop      \ ." temp is: " f.s cr
	read-humd throw drop      \ ." humd is: " f.s cr
	fswap fdup 10e f* f>d d>s \ ." stack :" .s ."  " f.s cr
	fdup 25e f-               \ ." stack :" .s ."  " f.s cr
	frot frot fover fover     \ ." stack :" .s ."  " f.s cr
	f/ 3 fpick                \ ." stack :" .s ."  " f.s cr
	f* fswap fdrop f+         \ ." stack :" .s ."  " f.s cr
	fswap fdrop               \ ." stack :" .s ."  " f.s cr
	10e f* f>d d>s
	false
    restore dup if 0 swap 0 swap then
    endtry ;
  

: cleanup ( -- nflag )
    i2c_handle @ bbbi2cclose true = ;


setup-htu21d throw
read-THcorrected throw
." H(%rh): " s>f 10e f/ 4 1 1 f.rdp cr
." T(c): " s>f 10e f/ 4 1 1 f.rdp cr
cleanup throw

bye