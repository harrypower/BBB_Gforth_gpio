/****************************************
 * Quick test of I2C routines
 * Temp/pressure measuring using BMP085
 ****************************************/

#include <stdio.h>
#include "i2cfunc.h"
#include "i2cfunc.c"

// we will use I2C2 which is enumerated as 1 on the BBB
#define I2CBUS 1

// set to 1 to print out the intermediate calculations
#define DBG_PRINT 0
// set to 1 to use the example values in the datasheet
// rather than the values retrieved via i2c
#define ALG_TEST 0

typedef struct bmp085_coeff_s
{
  short int ac1;
  short int ac2;
  short int ac3;
  unsigned short int ac4;
  unsigned short int ac5;
  unsigned short int ac6;
  short int b1;
  short int b2;
  short int mb;
  short int mc;
  short int md;
} bmp085_coeff_t;

const unsigned int conversion_delay[4]={6, 10, 15, 30};

int
main(void)
{
  int i;
  int handle;
  unsigned char tval;
  bmp085_coeff_t coeff;
  unsigned char *cbuf;
  unsigned char buf[10];
  
  unsigned char oss;
  long ut;
  long up;
  long x1, x2, x3;
  long b3, b4, b5, b6, b7;
  long p;
  long t;
  float deg;
  float kpa;
  
  oss=3; // pressure conversion mode: 3=ultra-high-res, down to 0 which is ultra-low-power
  cbuf=(unsigned char *)&coeff;
  
  handle=i2c_open(I2CBUS, 0x77);

  // read BMP085 coefficients (22 bytes)
  i2c_write_byte(handle, 0xaa);
  i2c_read(handle, cbuf, 22);

  // swap the order of bytes due to endianness
  for (i=0; i<22; i=i+2)
    {
      tval=cbuf[i];
      cbuf[i]=cbuf[i+1];
      cbuf[i+1]=tval;
    }
  
  // use example values from the datasheet to test the 
  // math formula below
  if (ALG_TEST)
    {
      coeff.ac1=408;
      coeff.ac2=-72;
      coeff.ac3=-14383;
      coeff.ac4=32741;
      coeff.ac5=32757;
      coeff.ac6=23153;
      coeff.b1=6190;
      coeff.b2=4;
      coeff.mb=-32768;
      coeff.mc=-8711;
      coeff.md=2868;
    }

  // display coeff table
  if (DBG_PRINT)
    {
      printf ("Coeff table:\n");
      printf("AC1 = %d\n", coeff.ac1);
      printf("AC2 = %d\n", coeff.ac2);
      printf("AC3 = %d\n", coeff.ac3);
      printf("AC4 = %d\n", coeff.ac4);
      printf("AC5 = %d\n", coeff.ac5);
      printf("AC6 = %d\n", coeff.ac6);
      printf("B1  = %d\n", coeff.b1);
      printf("B2  = %d\n", coeff.b2);
      printf("MB  = %d\n", coeff.mb);
      printf("MC  = %d\n", coeff.mc);
      printf("MD  = %d\n", coeff.md);
    }
  
  // read uncompensated temperature register
  buf[0]=0xf4;
  buf[1]=0x2e;
  i2c_write(handle, buf, 2);
  delay_ms(6);
  i2c_write_byte(handle, 0xf6);
  i2c_read(handle, buf, 2);
  ut=(((long)buf[0])<<8) | (long)buf[1];
  
  if (ALG_TEST)
    {
      ut=27898;
    }
  if (DBG_PRINT)
    {
      printf("UT  = %ld\n", ut);
    }
  // read uncompensated pressure register
  if (ALG_TEST)
    {
      oss=0;
    }
  buf[0]=0xf4;
  buf[1]=0x34+(oss<<6);
  i2c_write(handle, buf, 2);
  delay_ms(conversion_delay[oss]);
  i2c_write_byte(handle, 0xf6);
  i2c_read(handle, buf, 3);
  up=(((long)buf[0])<<16) | (((long)buf[1])<<8) | (long)buf[2];
  up=up>>(8-oss);
  
  if (ALG_TEST)
    {
      up=23843;
    }
  else if (DBG_PRINT)
    {
      printf("regF6=0x%02x\n", buf[0]);
      printf("regF7=0x%02x\n", buf[1]);
      printf("regF8=0x%02x\n", buf[2]);
    }
  if (DBG_PRINT)
    {
      printf("UP  = %ld\n", up);
    }
  // we are done with the I2C
  i2c_close(handle);
  
  // now do the calculations for temperature
  x1=(ut-(long)coeff.ac6)*((long)coeff.ac5)/32768;
  x2=((long)coeff.mc)*2048/(x1+(long)coeff.md);
  b5=x1+x2;
  t=(b5+8)/16;
  deg=((float)t)/10;
  
  if (DBG_PRINT)
    {
      printf("Temperature calc:\n");
      printf("X1  = %ld\n", x1);
      printf("X2  = %ld\n", x2);
      printf("B5  = %ld\n", b5);
      printf("T   = %ld\n", t);
    }
  
  // calculations for the pressure
  b6=b5-4000;
  x1=(((long)coeff.b2)*(b6*b6/4096))/2048;
  x2=((long)coeff.ac2)*b6/2048;
  x3=x1+x2;
  if (DBG_PRINT)
    {
      printf("Pressure calc:\n");
      printf("B1  = %ld\n", b6);
      printf("X1  = %ld\n", x1);
      printf("X2  = %ld\n", x2);
      printf("X3  = %ld\n", x3);
    }
  b3=(((((long)coeff.ac1)*4+x3)<<oss) +2)/4;
  x1=(((long)coeff.ac3)*b6)>>13;
  x2=(((long)coeff.b1)*(b6*b6>>12))>>16;
  x3=(x1+x2+2)>>2;
  b4=((long)coeff.ac4)*(unsigned long)(x3+32768)/32768;
  b7=((unsigned long)up - b3)*(50000>>oss);
  if (b7<0x80000000)
    p=(((unsigned long)b7)<<1)/b4;
  else
    p=(((unsigned long)b7)/b4)*2;
    
  if (DBG_PRINT)
    {
      printf("B3  = %ld\n", b3);
      printf("X1  = %ld\n", x1);
      printf("X2  = %ld\n", x2);
      printf("X3  = %ld\n", x3);
      printf("B4  = %ld\n", b4);
      printf("B7  = %ld\n", b7);
      printf("p   = %ld\n", p);
    }
  x1=(p/256);
  x1=x1*x1;  
  if (DBG_PRINT)
    {
      printf("X1  = %ld\n", x1);
    }
  x1=(x1*3038)>>16;
  x2=(-7357*p)>>16;
  p+=(x1+x2+3791)>>4;
  kpa=((float)p)/1000;
  if (DBG_PRINT)
    {
      printf("X1  = %ld\n", x1);
      printf("X2  = %ld\n", x2);
      printf("p  = %ld\n", p);
    }

  // print out the results
  printf("Temperature is %.1f degrees C\n", deg);
  printf("Pressure is %.3f kPa\n", kpa);

  return(0);
}


