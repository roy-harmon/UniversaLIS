# IMMULIS
An LIS system for Siemens IMMULITE, written in C#.

## Installation

The project will eventually be updated to include an executable setup file. For now, IMMULIS (working title) can be installed by navigating to the IMMULIS.exe file using the command prompt and enter the following commands:

```bash
installutil IMMULIS.exe
net start IMMULIService
```

Please note: The software also requires a database server in order to function properly. See the next section for details.

## Database

This Windows service is designed to connect to a database of your choice. Currently, only Microsoft SQL Server is supported, but MySQL and ODBC connection driver support is planned. Due to the deprecation of the native Oracle client in recent versions of the .NET framework, Oracle databases are not currently supportable without a third-party ODBC driver.
Whichever data source you use, just be sure to specify a valid connection string in the IMMULIS.exe.config file as discussed below.

While some parts of the database are fairly flexible, the IMMULIS service expects certain tables and fields to be present. To that end, several SQL "CREATE TABLE" scripts will be provided in an upcoming release.

## Configuration ##

The "IMMULIS.exe.config" file contains several configuration items that should be changed according to your specific requirements. 
Note: For settings marked with an asterisk (\*), ensure that the setting matches the analyzer's LIS Parameters configuration screen.

---
### Serial Port Parameters ###
*   SerialPortNum - Change this to the COM port of whichever serial port you intend to connect to the IMMULITE analyzer. Default = COM4.
* \*SerialPortBaudRate - Valid parameters include 1200, 2400, 4800 and 9600. Default is 9600. 
* \*SerialPortParity - None = 0 (default), Odd = 1, Even = 2.
* \*SerialPortDataBits - 7 or 8 (default).
* \*SerialPortStopBits - 1 (default) or 2.
### Other Parameters ###
* \*LIS_ID - LIS (default); match this field to the Receiver ID field on the analyzer's configuration screen.
* \*LIS_Password - (Blank by default)
*   ConnectionString - Database server connection string. Defaults to local SQLExpress instance with database "LISDB" and Windows authentication.
*   SenderAddress - The address of the LIS manufacturer. Currently blank.
*   SenderPhone - Yeah, like I want *more* robocalls.
* \*ReceiverID - IMMULITE (default); match this field to the Sender ID field on the analyzer config screen.
*   AutoSendOrders - True to automatically send all pending orders to the analyzer after a specified interval. Otherwise false (default).
*   AutoSendInterval - Number of milliseconds to wait between polling the database for orders and sending them to the analyzer if AutoSendOrders is true.

## Usage

The program runs as a Windows service under a Local System account. After installation, the service should automatically start on boot. Or rather, "Automatic (Delayed Start)" since some RS-232 port drivers take a moment to initialize.

This program will eventually support all modes of operation available on the IMMULITE. The active communication mode is determined by the "LIS Host Query Mode" setting in the analyzer's LIS Parameters configuration screen.
* For "Unidirectional" mode: 
  * Results are transmitted from the analyzer to the LIS and inserted into a database for storage and retrieval.
* For "Bidirectional" mode (WORK-IN-PROGRESS): 
  * Pending test requests are pulled from a database (specified in the config file) and sent to the analyzer.
  * These test orders are stored in the analyzer's Worklist pending assignment to a sample cup.
  * When the operator enters data into the "Accession #" field on the analyzer's Worklist Entry screen, the relevant patient and test order information is populated automatically if the entered value matches a sample number in the list sent by the LIS.
    * This data entry can be performed manually or by scanning a barcoded label. For efficiency and accuracy, the latter is encouraged.
  * Results are transmitted from the analyzer to the LIS and inserted into a database for storage and retrieval.
* For "Bidirectional Query" mode:
  * Like "Bidirectional" mode, but if the "Accession #" input does not match an existing record in the Worklist, the analyzer queries the LIS for any pending test orders for the sample.

NOTE: "Bidirectional mode" currently has some bugs involving resolution of the "contention state" wherein both LIS and analyzer attempt to start sending a message at the same time.

Please also note that "Control" and "Verify" samples may be supported in the future, but initially, the focus is entirely on patient samples.

## User Interface

A graphical user interface is not included at this time because most use cases will involve another application for management of patient information and test requests. Those applications (the good ones, anyway) are typically capable of interfacing with a SQL Server or MySQL database directly. Currently, the IMMULIS software only forms a bridge between the instrument and a database server; this database server can be accessed by third-party programs to automatically transmit test requests to the instrument and pull results into the reporting software.

A simple GUI might be added in the future, but mostly for demonstration purposes. The real benefit of this software is in its ability to integrate with other systems, so while a GUI could be used, it would be less efficient in a production environment.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## Acknowledgements

This software was written using information from the IMMULITE® Systems Interface Specification manual, downloaded from the [Siemens Healthineers Document Library](https://doclib.siemens-healthineers.com/document/592738). 
Support for MySQL uses the Oracle MySQL team's MySQL Connector/.NET 8.0, used under the GPLv2 license as outlined [here](https://downloads.mysql.com/docs/licenses/connector-net-8.0-gpl-en.pdf).
IMMULITE is a trademark of Siemens Healthcare Diagnostics.

## License

IMMULIS is published under the [MIT](https://choosealicense.com/licenses/mit/) license.

MIT License
---

Copyright (c) 2021 Roy Harmon

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
