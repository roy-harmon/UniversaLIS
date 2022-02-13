# UniversaLIS

An LIS system for ASTM/CLSI-compliant clinical laboratory instruments, written in C#. Originally created for Siemens IMMULITE systems, but expanded to work with any system that complies with ASTM-1381/1394 or CLSI-LIS1-A/LIS2-A2 specifications.

## Installation

The project will eventually be updated to include an executable setup file. It was originally registered as a Windows service using InstallUtil, but migration from .NET Framework 4.8 to .NET 5.0 removed service installers. Restoring this functionality is top priority, but until I figure out the new cross-platform way to do this, the program can be executed using the Visual Studio debugger.

Please note: The software also requires a database server in order to function properly. See the next section for details.

## Database

This Windows service is designed to connect to a database of your choice. Currently, only Microsoft SQL Server is supported, but MySQL and ODBC connection driver support is planned. Due to the deprecation of the native Oracle client in recent versions of the .NET framework, Oracle databases are not currently supportable without a third-party ODBC driver.
Whichever data source you use, just be sure to specify a valid connection string in the UniversaLIS.exe.config file as discussed below.

While some parts of the database are fairly flexible, the UniversaLIS service expects certain tables and fields to be present. To that end, several SQL "CREATE TABLE" scripts will be provided in an upcoming release.

## Configuration ##

The "config.yml" file contains several configuration items that should be changed according to your specific requirements. The details of each setting are described in the comments of that file.
Note: For settings marked with an asterisk (\*), ensure that the setting matches the analyzer's LIS Parameters configuration screen.

---

## Usage

The program runs as a Windows service under a Local System account. After installation, the service should automatically start on boot. Or rather, "Automatic (Delayed Start)" since some RS-232 port drivers take a moment to initialize.

This program will eventually support all modes of operation available on the IMMULITE. The active communication mode is determined by the "LIS Host Query Mode" setting in the analyzer's LIS Parameters configuration screen.
* For "Unidirectional" mode: 
  * Results are transmitted from the analyzer to the LIS and inserted into a database for storage and retrieval.
* For "Bidirectional" mode: 
  * Pending test requests are pulled from a database (specified in the config file) and sent to the analyzer.
  * These test orders are stored in the analyzer's Worklist pending assignment to a sample cup.
  * When the operator enters data into the "Accession #" field on the analyzer's Worklist Entry screen, the relevant patient and test order information is populated automatically if the entered value matches a sample number in the list sent by the LIS.
    * This data entry can be performed manually or by scanning a barcoded label. For efficiency and accuracy, the latter is encouraged.
  * Results are transmitted from the analyzer to the LIS and inserted into a database for storage and retrieval.
* For "Bidirectional Query" mode:
  * Like "Bidirectional" mode, but if the "Accession #" input does not match an existing record in the Worklist, the analyzer queries the LIS for any pending test orders for the sample.

Please note that "Control" and "Verify" samples may be supported in the future, but initially, the focus is entirely on patient samples.

## User Interface

A graphical user interface is not included at this time because most use cases will involve another application for management of patient information and test requests. Those applications (the good ones, anyway) are typically capable of interfacing with a SQL Server or MySQL database directly. Currently, the UniversaLIS software only forms a bridge between the instrument and a database server; this database server can be accessed by third-party programs to automatically transmit test requests to the instrument and pull results into the reporting software.

A simple GUI might be added in the future, but mostly for demonstration purposes. The real benefit of this software is in its ability to integrate with other systems, so while a GUI could be used, it would be less efficient in a production environment.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## Acknowledgements

This software was written using information from the IMMULITE® Systems Interface Specification manual, downloaded from the [Siemens Healthineers Document Library](https://doclib.siemens-healthineers.com/document/592738). 
Support for MySQL uses the Oracle MySQL team's MySQL Connector/.NET 8.0, used under the GPLv2 license as outlined [here](https://downloads.mysql.com/docs/licenses/connector-net-8.0-gpl-en.pdf).
IMMULITE is a trademark of Siemens Healthcare Diagnostics.

## License

UniversaLIS is published under the [MIT](https://choosealicense.com/licenses/mit/) license.

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
