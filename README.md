These Repo contains GPIO and I2C librarys for Beagle Bone Black Rev C hardware using Debian Wheezy Linux.
See the wiki on github.com for more information on these librarys.  There are also some examples for the GPIO
and the I2C usage.  This code does not deal at all with device tree access or managment.  I would suggest you use 
BoneScript pinMode() and getPinMode() functions to set up the device tree stuff directly.

Things to note:
- The i2c example BMP180_i2c.fs will work correctly with out a device tree set up as it uses i2c2 port at P9_19 and P9_20 header pins.  If however you use the bone script example to talke to the BMP180 or BMP85 device it appears that this code will then fail to work.  I do not at yet understand the reason for this but something seems not to be released in bone script after it has talked to the sensor.  
- GPIO access works good if you just want to turn pins on and off but be aware that due to the non determinancy of linux it is impossible to get timing of things perfect or consistent.  Unlike the above note about i2c, if you used bone script to set up a device tree for GPIO then this gforth code will access the pins just fine!