#define VERSION "v0.2"

#include "SPI.h"
#include "TFT_eSPI.h"

#define GC9A01_DRIVER

//splash screen
// you can use GIMP to export any 240x240 image to C code and then adapt it like this one
#include "oiltemp_splashscreen.c"
#define IMG_WIDTH 240
#define IMG_HEIGHT 240

// Fast drawing font library
#include <RREFont.h>
#include <rre_arialdig47b.h>
#include <rre_arialb_16.h>

// Bluetooth LE stack library
#include <ArduinoBLE.h>

#include <Adafruit_ADS1X15.h>
Adafruit_ADS1115 ads;  /* Use this for the 16-bit version */

//Adafruit_ADS1015 ads;     /* Use this for the 12-bit version */
#define SERIESRESISTOR 10000
#define THERMISTORNOMINAL 10000
#define TEMPERATURENOMINAL 25   
#define BCOEFFICIENT 3443

// D-Pins for the Display SCK -> 13, SDA/MOSI -> 11
#define TFT_MISO -1
#define TFT_MOSI D11
#define TFT_SCLK D13
#define TFT_CS    D9  // Chip select control pin
#define TFT_DC    D8  // Data Command control pin
#define TFT_RST   D7  // Reset pin (could connect to RST pin)
#define TFT_BL D6
#define TFT_BACKLIGHT_ON HIGH

#define LOAD_GLCD   // Font 1. Original Adafruit 8 pixel font needs ~1820 bytes in FLASH
#define LOAD_FONT2  // Font 2. Small 16 pixel high font, needs ~3534 bytes in FLASH, 96 characters
#define LOAD_FONT4  // Font 4. Medium 26 pixel high font, needs ~5848 bytes in FLASH, 96 characters
#define LOAD_FONT6  // Font 6. Large 48 pixel font, needs ~2666 bytes in FLASH, only characters 1234567890:-.apm
#define LOAD_FONT7  // Font 7. 7 segment 48 pixel font, needs ~2438 bytes in FLASH, only characters 1234567890:.
#define LOAD_FONT8  // Font 8. Large 75 pixel font needs ~3256 bytes in FLASH, only characters 1234567890:-.
#define LOAD_GFXFF  // FreeFonts. Include access to the 48 Adafruit_GFX free fonts FF1 to FF48 and custom fonts
#define SMOOTH_FONT

#define TFT_WIDTH 240
#define TFT_HEIGHT 240

#define SPI_FREQUENCY  40000000
#define SPI_READ_FREQUENCY  20000000
#define SPI_TOUCH_FREQUENCY  2500000

// Use hardware SPI
TFT_eSPI tft = TFT_eSPI();

#ifdef ESP32
#undef F
#define F(s) (s)
#endif

#ifdef ESP32
  #define LEDR LED_RED
  #define LEDG LED_GREEN
  #define LEDB LED_BLUE
#endif

int32_t w, h, n, n1, cx, cy, cx1, cy1, cn, cn1;
uint8_t tsa, tsb, tsc, ds;

// Meter colour schemes
#define RED2RED 0
#define GREEN2GREEN 1
#define BLUE2BLUE 2
#define BLUE2RED 3
#define GREEN2RED 4
#define RED2GREEN 5
#define BLUE2GREEN2RED 6

#define GC9A01_GREY 0x2124 // Dark grey 16 bit colour other values: 0x528a or 0x2104

// global font variable
RREFont rre_font;

// needed for RREFont library initialization, define your fillRect
void customRect(int x, int y, int w, int h, int c) { return tft.fillRect(x, y, w, h, c); }

uint32_t runTime = -99999;       // time for next update

int temperature_reading = 0; // Value to be displayed
int d = 90; // Variable used for the sinewave test waveform
int last_value = -999;
int text_colour = 0; // To hold the current text colour

BLEService oiltempService("180C");  // User defined service

BLEIntCharacteristic oiltempCharacteristic("2A56",  BLERead); // remote clients will only be able to read this

long previousMillis = 0;

//#define TEMP_SIMULATION // use sinus wave as temp simulation for testing
#define USE_INBUILT_RGB_LED_FOR_TEMPERATURE // use inbuilt nano rgb led to show temperature blue = cold red = hot

void setup()
{
  pinMode(TFT_BL, OUTPUT);
  digitalWrite(TFT_BL, LOW);

  Serial.begin(9600);
 // while(!Serial);
  
  Serial.println("Starting OilMeter Jan");

  uint8_t start_setup_millis = millis();

  tft.init();

  rre_font.init(customRect, tft.width(), tft.height()); // custom fillRect function and screen width and height values
  
  rre_font.setFont(&rre_arialb_16);
  
  tft.setSwapBytes(true); 
  tft.pushImage(0, 0, IMG_WIDTH, IMG_HEIGHT, SPLASHSCREEN);

  rre_font.setColor(TFT_WHITE, 0);
  rre_font.printStr(100,200,VERSION);

  digitalWrite(TFT_BL, HIGH);

  w = tft.width();
  h = tft.height();
  n = min(w, h);
  n1 = n - 1;
  cx = w / 2;
  cy = h / 2;
  cx1 = cx - 1;
  cy1 = cy - 1;
  cn = min(cx1, cy1);
  cn1 = cn - 1;
  tsa = ((w <= 176) || (h <= 160)) ? 1 : (((w <= 240) || (h <= 240)) ? 2 : 3); // text size A
  tsb = ((w <= 272) || (h <= 220)) ? 1 : 2;                                    // text size B
  tsc = ((w <= 220) || (h <= 220)) ? 1 : 2;                                    // text size C
  ds = (w <= 160) ? 9 : 12;                                                    // digit size

  rre_font.setFont(&rre_ArialDig47b);
  
  #ifdef ARDUINO_ARDUINO_NANO33BLE
    pinMode(LED_BUILTIN, OUTPUT); // initialize the built-in LED pin
  #endif

  if (!BLE.begin()) {   // initialize BLE
    Serial.println("starting BLE failed!");
    while (1);
  }

  // adc init
  ads.setGain(GAIN_TWOTHIRDS);
  if (!ads.begin()) {
    Serial.println("Failed to initialize ADS.");
    while (1);
  }

  BLE.setLocalName("OiltempJan");  // Set name for connection
  BLE.setAdvertisedService(oiltempService); // Advertise service
  oiltempService.addCharacteristic(oiltempCharacteristic); // Add characteristic to service
  BLE.addService(oiltempService); // Add service
  oiltempCharacteristic.writeValue(0); // Set default initial value

  BLE.advertise();  // Start advertising
  
  Serial.print("Peripheral device MAC: ");
  Serial.println(BLE.address());
  Serial.println("Waiting for connections...");

  // Bootup Splash Screen 5seconds but calculate already used time in, so that in sum bootup is 5s
  delay(5000-(millis()-start_setup_millis));

  tft.fillScreen(TFT_BLACK);

  last_value = 130;
  ringMeter(0, 0, 130, 0, 5, 120, BLUE2GREEN2RED);
  last_value = -999;
}

static inline uint32_t micros_start() __attribute__((always_inline));
static inline uint32_t micros_start()
{
  uint8_t oms = millis();
  while ((uint8_t)millis() == oms)
    ;
  return micros();
}

void loop(void)
{ 
  BLEDevice central = BLE.central();  // Wait for a BLE central to connect
  
  // if a central is connected to the peripheral:
  if (central) {
    Serial.print("Connected to central MAC: ");
    // print the central's BT address:
    Serial.println(central.address());

    // turn on the LED to indicate the connection:
    #ifdef ARDUINO_ARDUINO_NANO33BLE
      digitalWrite(LED_BUILTIN, HIGH);
    #endif
    

    while (central.connected()){
      if (millis() - runTime >= 1000L) { // Execute every 2s
        runTime = millis();

        #ifdef TEMP_SIMULATION
          d += 3; if (d >= 360) d = 0;
          temperature_reading = 60 + (75 * sineWave(d + 60));
        #else
          // Serial.print("readCelsius 1 ");
          // Serial.println(readTemperatureCelsius());
          temperature_reading = readTemperatureCelsius();
        #endif

        redrawScreenGauge(temperature_reading);
      }

      long currentMillis = millis();
      // if 200ms have passed, check the battery level:
      if (currentMillis - previousMillis >= 500) {
        previousMillis = currentMillis;

        //Serial.println(value);
        oiltempCharacteristic.writeValue( temperature_reading );
      }

      
    } // keep looping while connected
    
    // when the central disconnects, turn off the LED:
    #ifdef ARDUINO_ARDUINO_NANO33BLE
      digitalWrite(LED_BUILTIN, LOW);
    #endif
    
    Serial.print("Disconnected from central MAC: ");
    Serial.println(central.address());
  } else {
    if (millis() - runTime >= 1000L) { // Execute every 2s
      runTime = millis();
      
      #ifdef TEMP_SIMULATION
          d += 3; if (d >= 360) d = 0;
          temperature_reading = 70 + (80 * sineWave(d+60));
      #else
        // Serial.print("readCelsius 2 ");
        // Serial.println(readTemperatureCelsius());
        temperature_reading = readTemperatureCelsius();
      #endif

      redrawScreenGauge(temperature_reading);
    }
  }

}

float last_tempval = -999;

int readTemperatureCelsius()
{
  if(last_tempval <= -999)
    last_tempval = readTemperatureFromSensor();

  float hysteresis = 0.3;

  float temp = readTemperatureFromSensor();
  if( abs(temp-last_tempval) > hysteresis )
  {
      last_tempval = readTemperatureFromSensor();
      return round(temp);
  }
  else
    return round(last_tempval);
}

float readTemperatureFromSensor()
{
  int16_t adc0 = ads.readADC_SingleEnded(0);
  int16_t adc1 = ads.readADC_SingleEnded(1);

  // Serial.print("adc0 "); 
  // Serial.println(adc0);

  // Serial.print("adc1 "); 
  // Serial.println(adc1);

  float val = (float(adc1) / float(adc0)) - 1;

  if( val <= 0.001 && val >= -0.001)
    return -48.0;

  float resistance = SERIESRESISTOR / val;

  // Serial.print("Thermistor resistance "); 
  // Serial.println(resistance);

  float steinhart;
  steinhart = resistance / THERMISTORNOMINAL;   
  steinhart = log(steinhart);                 
  steinhart /= BCOEFFICIENT;                  
  steinhart += 1.0 / (TEMPERATURENOMINAL + 273.15);
  steinhart = 1.0 / steinhart;              
  steinhart -= 273.15;

  return steinhart;
}

void redrawScreenGauge(int reading) {
      // Serial.print("redrawScreenGauge ");
      
      // Serial.println(reading);
      int xpos = 0, ypos = 5, radius = 120;
      xpos = ringMeter(reading, 0, 130, xpos, ypos, radius, BLUE2GREEN2RED); // Draw analogue meter
}

void drawInbuiltRGBLed(int color_16bit)
{
  byte r = byte(((color_16bit & 0xF800) >> 11) << 3);
  byte g = byte(((color_16bit & 0x7E0) >> 5) << 2);
  byte b = byte(((color_16bit & 0x1F)) << 3);

  analogWrite(LEDR, r ^ 255);
  analogWrite(LEDG, g ^ 255);
  analogWrite(LEDB, b ^ 255);
}

// #########################################################################
//  Draw the meter on the screen, returns x coord of righthand side
// #########################################################################
int ringMeter(int value, int vmin, int vmax, int x, int y, int r, byte scheme)
{
  // dont redraw if nothing has changed
  if( value == last_value )
    return x+r;  
  // Minimum value of r is about 52 before value text intrudes on ring
  // drawing the text first is an option
  
  x += r; y += r;   // Calculate coords of centre of ring
  int w = r / 3;    // Width of outer ring is 1/3 of radius
  int angle = 150;  // Half the sweep angle of meter (300 degrees)
  int v = vmin;
  int v_last = vmin;

  if( value <= vmin )
  {
    v = map(vmin, vmin, vmax, -angle, angle); // Map the value to an angle v
  }
  else if (value >= vmax)
  {
    v = map(vmax, vmin, vmax, -angle, angle); // Map the value to an angle v
  } 
  else
  {
    v = map(value, vmin, vmax, -angle, angle); // Map the value to an angle v
  }
  v_last = map(last_value, vmin, vmax, -angle, angle); // Map the value to an angle v
    
  
  // byte seg = 3; // Segments are 5 degrees wide = 60 segments for 300 degrees
  // byte inc = 3; // Draw segments every 5 degrees, increase to 10 for segmented ring
  byte seg = 3; // Segments are 5 degrees wide = 60 segments for 300 degrees
  byte inc = 3; // Draw segments every 5 degrees, increase to 10 for segmented ring
  //byte seg = 5;
  //byte inc = 5;
  // Draw colour blocks every inc degrees
  int angle_low; 
  int angle_high;
  if( v < v_last)
  {
    angle_low = v - (v % seg);
    angle_high = v_last + seg - (v_last % seg);
  } else {
    angle_low = v_last - (v_last % seg);
    angle_high = v + seg - (v % seg);
  }

  if(angle_low < -angle)
    angle_low = -angle;

  if(angle_high > angle)
    angle_high = angle;

  text_colour = blue_to_green_to_red(v, angle, vmin, vmax);
  //for (int i = -angle; i < angle; i += inc) {
  for (int i = angle_low; i < angle_high; i += inc) {
    // Choose colour from scheme
    int colour = 0;
    
    // Calculate pair of coordinates for segment start
    float sx = cos((i - 90) * 0.0174532925);
    float sy = sin((i - 90) * 0.0174532925);
    uint16_t x0 = sx * (r - w) + x;
    uint16_t y0 = sy * (r - w) + y;
    uint16_t x1 = sx * r + x;
    uint16_t y1 = sy * r + y;

    // Calculate pair of coordinates for segment end
    float sx2 = cos((i + seg - 90) * 0.0174532925);
    float sy2 = sin((i + seg - 90) * 0.0174532925);
    int x2 = sx2 * (r - w) + x;
    int y2 = sy2 * (r - w) + y;
    int x3 = sx2 * r + x;
    int y3 = sy2 * r + y;

    if (i < v) { // Fill in coloured segments with 2 triangles
      switch (scheme) {
      case 0: colour = TFT_RED; break; // Fixed colour
      case 1: colour = TFT_GREEN; break; // Fixed colour
      case 2: colour = TFT_BLUE; break; // Fixed colour
      case 3: colour = rainbow(map(i, -angle, angle, 0, 127)); break; // Full spectrum blue to red
      case 4: colour = rainbow(map(i, -angle, angle, 63, 127)); break; // Green to red (high temperature etc)
      case 5: colour = rainbow(map(i, -angle, angle, 127, 63)); break; // Red to green (low battery etc)
      case 6: colour = blue_to_green_to_red(i, angle, vmin, vmax); break; // Red to green (low battery etc)
      default: colour = TFT_BLUE; break; // Fixed colour
      }
    
      tft.fillTriangle(x0, y0, x1, y1, x2, y2, colour);
      tft.fillTriangle(x1, y1, x2, y2, x3, y3, colour);
      //if( value >= vmin && value <= vmax)
      text_colour = colour; // Save the last colour drawn
    }
    else // Fill in blank segments
    {
      tft.fillTriangle(x0, y0, x1, y1, x2, y2, GC9A01_GREY);
      tft.fillTriangle(x1, y1, x2, y2, x3, y3, GC9A01_GREY);
    }
  }
  
  char buff[6];
  sprintf(buff, "%3d' ", value);
  rre_font.setColor(text_colour, 0);
  
  //font.setScale(1.5,1.5);
  rre_font.printStr(x - 50,y - 25,buff);
  
  #ifdef USE_INBUILT_RGB_LED_FOR_TEMPERATURE
    drawInbuiltRGBLed(text_colour);
  #endif
  
  last_value = value;
  return x + r;
}

// #########################################################################
// Return a 16 bit rainbow colour
// #########################################################################
unsigned int rainbow(byte value)
{
  // Value is expected to be in range 0-127
  // The value is converted to a spectrum colour from 0 = blue through to 127 = red

  byte red = 0; // Red is the top 5 bits of a 16 bit colour value 0-31
  byte green = 0;// Green is the middle 6 bits 
  byte blue = 0; // Blue is the bottom 5 bits

  byte quadrant = value / 32;

  if (quadrant == 0) {
    blue = 31;
    green = 2 * (value % 32);
    red = 0;
  }
  if (quadrant == 1) {
    blue = 31 - (value % 32);
    green = 63;
    red = 0;
  }
  if (quadrant == 2) {
    blue = 0;
    green = 63;
    red = value % 32;
  }
  if (quadrant == 3) {
    blue = 0;
    green = 63 - 2 * (value % 32);
    red = 31;
  }
  return (red << 11) + (green << 5) + blue;
}

unsigned int blue_to_green_to_red(int i, int angle, int vmin, int vmax)
{
  byte red = 0; // Red is the top 5 bits of a 16 bit colour value
  byte green = 0;// Green is the middle 6 bits
  byte blue = 0; // Blue is the bottom 5 bits
  
  // map angle i to value
  byte value = map(i, -angle, angle, vmin, vmax);

  // blue until 50
  if( value < 10 ) {
    blue = 31;
    green = 0;
    red = 0;
  }

  if( value >= 10 && value <= 70 ) {
      float percentage = (value - 10) / 60.0;
      if( percentage <= 0.5 )
      {
        blue = 31;
        green = round(63*percentage*2);
      } else
      {
        blue = 31 - round(31*(percentage-0.5)*2);
        green = 63;
      }
      
      red = 0;
  }

  if( value > 70 && value < 90 ) {
    blue = 0;
    green = 63;
    red = 0;
  }

  if( value >= 90 && value <= 120 ) {
      float percentage = (value - 90) / 30.0;
      blue = 0;
      if( percentage < 0.5 )
      {
        green = 63;
        red = round(31*(percentage*2));
      } else
      {
        green = 63 - round(63*(percentage-0.5)*2);
        red = 31;
      }
      
  }

  if( value > 120 ) {
    blue = 0;
    green = 0;
    red = 31;
  }

  return (red << 11) + (green << 5) + blue;
}

// #########################################################################
// Return a value in range -1 to +1 for a given phase angle in degrees
// #########################################################################
float sineWave(int phase) {
  return sin(phase * 0.0174532925);
}