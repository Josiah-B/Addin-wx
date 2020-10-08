## Description
Here's a .net 3.5 WPF application designed to display weather data from multiple personal Automatic weather stations (Davis, Peet Brothers).

See www.addinwx.com for screenshots.

### Background
Back around 2012, we purchased our first real weather station (an Ultimeter 2100 from PeetBros with pro sensors) and shortly thereafter started looking for software to interface to it. What we were looking for was something that would display the data in a nice/easy to read way while still having a few good features. We started with the "WeatherText Tools" software that came with the Station then moved to software (of which I don't remember the name of) originally available at N3FJP's website back in 2012. After that, we tried 3 or 4 other pieces of software before finally giving up. Our biggest complaint with the existing software was that they either didn't display the Station data very well, or the configuration was messy.

Being new to programming, I decided to try to create what I couldn’t find in other software packages. Since then, the project has grown, the program has been rewritten a couple of times, and more features have been added.

### Features
- Across-the-Room visibility
- Changeable background image
- Connects to multiple weather stations
- Selectable units of measure (Fahrenheit/Celsius, Mph/Kph, InHg/mbar, etc.)
- Runs on Windows 7, 8, 8.1, and Windows 10

### Uploads to the following weather networks (via plugins):
- CWOP
- WUnderground

### Supports the following weather stations
- Davis
  - Vantage Vue, Pro, and Pro2. Via USB, Serial, and TCP Dataloggers.
- PeetBros.
  - Ultimeter models 2100, 800 and 100 (2005 and later)
