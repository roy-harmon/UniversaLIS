# UniversaLIS

An LIS system for ASTM/CLSI-compliant clinical laboratory instruments, written in C#. Originally created for Siemens IMMULITE systems, but expanded to work with any system that complies with ASTM-1381/1394 or CLSI-LIS1-A/LIS2-A2 specifications.

The UniversaLIS project handles communication with laboratory instruments, transmitting data between connected analyzers and an internal SQLite database. The REST-LIS project (see below) has been added to enable communication via REST API for transmitting data to and from the SQLite database.

### DISCLAIMER 
This project was created to help streamline workflows in laboratories with limited access to paid alternatives, making healthcare more accessible to all. That said, it is still under development and has not been exhaustively validated. Please use this software wisely, and test it as thoroughly as possible with your hardware and software before relying on its performance in a production environment. I do my best to provide quality software, but I can't be certain there are no bugs. Therefore, I make no guarantees and you use this software at your own risk.

## Installation

The project can be registered as a Windows service, but the migration from .NET Framework to .NET Core removed service installers. This functionality should be restored now, and it can be registered as a service using `sc create` (see [the MSDN article](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/sc-create)).

The program can also be executed using the Visual Studio debugger.

Please note: The software no longer requires a database server in order to function properly. 

## Database

This program now uses an internal SQLite database, so it is no longer dependent on an external database server. Instead, it accepts test requests and reports results via a REST API (working title: "REST-LIS").

## Configuration

The "config.yml" file contains several configuration items that should be changed according to your specific requirements. The details of each setting are described in the comments of that file.
Note: For settings marked with an asterisk (\*), ensure that the setting matches the analyzer's LIS Parameters configuration screen.

## API
The UniversaLIS software forms a bridge between clinical analyzers (via serial port or TCP socket) and a web API; that web API can be accessed by third-party programs to automatically transmit test requests to the instrument and pull results into the reporting software.

This program was originally designed to connect clinical laboratory analyzers to databases, but it has been overhauled to replace the database server with a REST API. 
Applications can interact with the LIS via REST API calls; for ease of integration with third-party programs, the OpenAPI specification can be used to easily generate code in your choice of programming language. 

The OpenAPI specification will be provided along with more details as the various endpoints are fully implemented. This feature is currently in active development.

Please note that the original functionality (monitoring external database tables for test requests and populating tables with test results) has been removed but may be recreated as a separate utility in the future.

---

## Usage

The program runs as a Windows service under a Local System account. After installation, the service should automatically start on boot. Or rather, "Automatic (Delayed Start)" since some RS-232 port drivers take a moment to initialize.

This program supports the following modes of operation available on the IMMULITE (and presumably other analyzers). The active communication mode is determined by the "LIS Host Query Mode" setting in the IMMULITE's LIS Parameters configuration screen. Check the manufacturer's documentation for more information on your instrument's configuration.
* For "Unidirectional" mode (sometimes called "Receive-Only"): 
  * Results are transmitted from the analyzer to the LIS and inserted into a database for storage and retrieval.
* For "Bidirectional" mode (or "Send and Receive"): 
  * Pending test requests are pulled from a database (specified in the config file) and sent to the analyzer.
  * These test orders are stored in the analyzer's Worklist pending assignment to a sample cup.
  * When the operator enters data into the "Accession #" field on the analyzer's Worklist Entry screen, the relevant patient and test order information is populated automatically if the entered value matches a sample number in the list sent by the LIS.
    * This data entry can be performed manually or by scanning a barcoded label. For efficiency and accuracy, the latter is encouraged.
  * Results are transmitted from the analyzer to the LIS and inserted into a database for storage and retrieval.
* For "Bidirectional Query" mode:
  * Like "Bidirectional" mode, but if the "Accession #" input does not match an existing record in the Worklist, the analyzer queries the LIS for any pending test orders for the sample.

Please note that "Control" and "Verify" samples may be supported in the future, but initially, the focus is entirely on patient samples.

## User Interface

A graphical user interface is not included at this time because most use cases will involve another application for management of patient information and test requests. 

A simple GUI might be added in the future, but mostly for demonstration purposes. The real benefit of this software is in its ability to integrate with other systems, so while a GUI could be used, it would be less efficient in a production environment.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## Acknowledgements

This software was written from scratch using information from the IMMULITE® Systems Interface Specification manual, downloaded from the [Siemens Healthineers Document Library](https://doclib.siemens-healthineers.com/document/592738). 
IMMULITE is a trademark of Siemens Healthcare Diagnostics.

## License

UniversaLIS is published under the [MIT](https://choosealicense.com/licenses/mit/) license.

MIT License
---

Copyright (c) 2020-2023 Roy Harmon

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
