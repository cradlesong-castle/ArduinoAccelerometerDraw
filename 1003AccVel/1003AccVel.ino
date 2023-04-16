 
#include <TinyScreen.h>
#include <SPI.h>
#include <Wire.h>         // For I2C communication with sensor
#include "BMA250.h"       // For interfacing with the accel. sensor
#include <stdlib.h>

/*DEFINE ALL THE COLOURS*/
#define BLACK           0x00
#define WHITE           0xFF
#define RED             0x03f
#define ORANGE          0x0F
#define YELLOW          0x1F
#define GREEN           0x1C
#define BLUE            0xE0
#define PURPLE          0xEF
#define BROWN           0x32

/*DEFINE BUTTONS*/
#define COLOUR_PREV_BTN 1
#define DRAW_BTN 2
#define UNDO_BTN 4
#define COLOUR_NEXT_BTN 8

/*SET UP COLOURS AND SCREEN*/
#define NUM_OF_COLOURS 9
int colour_index = 0;
int colour_list[10] = {WHITE, WHITE, RED, ORANGE, YELLOW, GREEN, BLUE, PURPLE, BROWN};
char *colour_name[10] = {"BLA", "WHI", "RED", "ORA", "YEL", "GRE", "BLU", "PUR", "BRO"};
int colour_cycle = 0; //To regulate the time between colours. (So user doesn't scroll too much by accident)
TinyScreen display = TinyScreen(0);

/* DEFINE ACCELEROMETER READING INTERVAL IN MILISECONDS */
#define INTERVAL 18

#if defined(ARDUINO_ARCH_SAMD)
#define SerialMonitorInterface SerialUSB
#else
#define SerialMonitorInterface Serial
#endif

/* Accelerometer sensor variables for the sensor and its values */
BMA250 accel_sensor;
const int period = 10;                                       // raw readings taken before average & SD is produced
float cal_sensi = 10;                                        // If SD is lower than this, input is taken to be 0
float x, y, z;                                                // raw accel readings
int * x_array = (int *) calloc (period, sizeof(int));       // to store raw readings to calculate average
int * y_array = (int *) calloc (period, sizeof(int));
int * z_array = (int *) calloc (period, sizeof(int));
int x_ref, y_ref, z_ref;                                    // to cancel offsets due to gravity
int x_fil, y_fil, z_fil;                                    // filtered accelerometer reading (average/smoothed accelerometer readings)
float x_vel, y_vel, z_vel = 0;                                // to store velocities of each axis
int old_millis; 
int new_millis;
char xData[6];
char yData[6];
char zData[6];
/*
* SET UP DEFAULT SCREEN
* This function runs on setup to print out default screen
*/
void setup_screen(){

  /*SET SCREEN TO BE BLACK*/
  display.setFont(liberationSans_12ptFontInfo);
  display.fontColor(BLACK,BLACK);
  for(int i=0;i<5; i++)
  {
    display.setCursor(0,i*12);
    display.print("                    ");
  }

  // set font and font colour
  display.setFont(liberationSans_8ptFontInfo);
  display.fontColor(WHITE,BLACK);

  //print the top left
  display.setCursor(0,10);
  display.print("Draw");

  //print the top right
  display.setCursor(70,10);
  display.print("Undo");

  //print the bottom left
  display.setCursor(0,45);
  display.print("Prev");

  //print the bottom right
  display.setCursor(70,45);
  display.print("Next");

  //Print initial colour
  display.setCursor(37,45);
  display.print(colour_name[colour_index]);
}

void setup() {
  // Typical Baud rate is 9600
  //SerialMonitorInterface.begin(9600);
  Serial.begin(9600);
  Wire.begin();
  display.begin();
  setup_screen();
  // Set up the BMA250 acccelerometer sensor
  // Left side is sensitivity, right side is update frequency. See BMA250.h file tab above.
  accel_sensor.begin(BMA250_range_2g, BMA250_update_time_05ms);
  old_millis = millis(); 

}

void loop() {
  float diff_millis;
 

  get_readings();
  if (array_is_full(x_array)) {
    filter_readings();
    calibrate();
    data_to_unity();
  }
  new_millis = millis();
  diff_millis = new_millis - old_millis;
  x_vel = (x_fil-x_ref) * (float)(diff_millis / 1000);
  y_vel = (y_fil-y_ref) * (float)(diff_millis / 1000);
  z_vel = (z_fil-z_ref) * (float)(diff_millis / 1000);

  old_millis = new_millis;

  delay(INTERVAL);
}

/*
* FUNCTION TO CHANGE COLOUR BASED ON BUTTON PRESSED
*/
void change_colour(char* data, const int displace){
  colour_index+=displace;                                   // Displace the index by displace variable
    if (colour_index<0){                                    // If displacing makes it -1, then change the value to the maximum index instead
      colour_index = NUM_OF_COLOURS-1;
    }
    else if (colour_index>(NUM_OF_COLOURS-1)) {             //If displacing makes it more than the maximum, then change the value to 0
      colour_index = 0;
    }
    colour_cycle = 0;                                       //Reset the time cycle

    // Print the current colour
    display.setCursor(37,45);
    display.print("         ");
    display.fontColor(colour_list[colour_index],BLACK);
    display.setCursor(37,45);
    display.print(colour_name[colour_index]);
}

/* THIS FUNCTION CHECKS IF AN ARRAY IS FULL. RETURNS A 0 OR 1. */
int array_is_full(int * array) {
    for (int i = 0; i < period; i++) {
        if (array[i] == 0)
            return 0;
    }
    return 1;
}


void append(int * array, int to_append) {
    if (array_is_full(array)) {
        for (int i = 1; i < period; i++) {
            array[i - 1] = array[i];
        }
        array[period - 1] = to_append;
    }
    else {
        for (int i = 0; i < period; i++) {
            if (array[i] == 0) {
                array[i] = to_append;
                break;
            }
        }
    }
}

/* THIS FUNCTION RETURNS THE MEAN OF AN ARRAY*/
int get_sma(int * array) {
  // to get mean
    int result = 0;
    for (int i = 0; i < period; i++) {
        result += array[i];
    }
    return result / period;
}

/* THIS FUNCTION RETURNS THE STANDARD DEVIATION OF AN ARRAY */
float get_SD(int* array) {
  // to get SD
    float result = 0;
    float mean = get_sma(array);
    for (int i = 0; i < period; i++) {
        result += pow(array[i]-mean,2);
    }
    if (isnan(sqrt(result/period-1))) {
      return 0;
    }
    else {
      return sqrt(result/period-1);
    }
}

/* 
* THIS FUNCTION CHECKS IF THE SD OF AN ARRAY IS LESS THAN A CERTAIN AMOUNT
* IF IT IS, THE CURRENT VELOCITY IS NEUTRALISED
*/
void calibrate(void) {
  //Gets offsets due to gravity if no motion detected
  if (get_SD(x_array) < cal_sensi) {
    x_ref = x_fil;
    x_vel -= x_vel;
  }
  if (get_SD(y_array) < cal_sensi) {
    y_ref = y_fil;
    y_vel -= y_vel;
  }
  if (get_SD(z_array) < cal_sensi) {
    z_ref = z_fil;
    z_vel -= z_vel;
  }
}


/*
 * SIMPLE ACCESSING AND READING OF DATA
 * DATA IS APPENDED TO ARRAYS TO CALCULATE MEAN AND SD
 */
void get_readings(void) {
  //This function gets new data from the acccelerometer
  // Get the acceleration values from the sensor and store them into global variables
  accel_sensor.read();
  x = accel_sensor.X;
  y = accel_sensor.Y;
  z = accel_sensor.Z;
  append(x_array, x);
  append(y_array, y);
  append(z_array, z);
}


/*
 *  CALLS THE FUNCTION TO AVERAGE READINGS
 */
void filter_readings(void) {
  // gets actual acceleration value, disregarding static acceleration from gravity
  x_fil = get_sma(x_array);
  y_fil = get_sma(y_array);
  z_fil = get_sma(z_array);
}

/*
 *  CONSIDERS CURRENT VARIABLES AND CONCATENATES DATA BEFORE SENDING TO UNITY VIA SERIAL 
 */
void data_to_unity(void) {
  int btn_num=display.getButtons();                             // Get the button being pressed
  if (btn_num!=1 && btn_num!=2 && btn_num!=4 && btn_num!=8 ){   // If there are multiple buttons being pressed, then set is as 0
    btn_num=0;
  }
  char data[25];                                                //Store the data in a string
  
  sprintf(data,"%d,", btn_num);
  
  sprintf(xData, "%d", x_fil-x_ref);                           // x-axis in unity, x-axis here
  strcat(data, xData);
  sprintf(yData, ",%d", z_fil-z_ref);                           // y-axis in unity, z-axis here
  strcat(data, yData);
  sprintf(zData, ",%d", y_fil-y_ref);                           // z-axis in unity, y-axis here
  strcat(data, zData);


  if(btn_num==COLOUR_PREV_BTN){                                  // BUTTON FUNCTIONS BEGIN HERE
    if (colour_cycle>=5){
      change_colour(data, -1);
    }

  }
  else if(btn_num==COLOUR_NEXT_BTN){
    if (colour_cycle>=5){
      change_colour(data, 1);
    }
  }

  char str_colour_index[2];
  sprintf(str_colour_index, ",%d", colour_index);         // Change the colour index to a string
  strcat(data, str_colour_index);                         // Add the colour index to the end of the string
  
  //Send data to unity
  //Serial.println(x_fil-x_ref);
  //Serial.println(z_fil-z_ref);
  Serial.println(data);                                   // Send data
  Serial.flush();                                         // Wait for data to finish sending
  if (colour_cycle<5){                                    // Only add when less than 5 to prevent number from getting too large
    colour_cycle++;
  } 
}
