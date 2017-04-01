# Gridcoin Snapshot Generator
This is a tool that creates a snapshot of the Gridcoin Blockchain. The software works as follows:
1. It will first close the wallet properly to prevent corruption.
2. It will launch 7zip and compress the txleveldb and the blk0001.dat file
3. Optionally, create MD5, SHA256 and/or SHA512 checksums
4. Optionally, upload the snapshot to Google Drive (see Google Drive Integration below)
3. Once the compression is finished, it will launch the wallet again.

# Timer
You can specify the amount of hours to wait between snapshots. It will use the specified filename with the date and time appended to it at the end.

# Filename
The filename format is as follows:  
**Your Specified Name - Year-Month-Day Hour-Minutes-Seconds AM/PM.7z**

# Checksums
The software can generate checksums for the Snapshot using MD5, SHA256 and SHA512. You can create all 3 checksums or just the one you specify. Simply select the Create Checksum checkbox and the desired hash types to use

# Google Drive integration
The software can upload the snapshots and the checksums to Google Drive. Before using it, you must have a file called client_secret.json in the same location where this software is located. To get the client_secret.json file, go to https://cloud.google.com/console and make a Google Drive API key. For more information on how to create the Google Drive API key as well as downloading the client_secret.json file, please go here: https://developers.google.com/drive/v3/web/quickstart/dotnet and follow Step 1.

# Requisites
You must have 7-zip installed in the C:\Program Files\7-zip folder. This software launches the 7z.exe command line software to compress the blockchain.

# FAQ
*I get an error about 7zip.  
-The software must likely is not installed in C:\Program Files\7-zip. Please install it to that directory

*I get the message "GridcoinResearch.exe could not be found. Please launch the wallet manually"  
-The wallet is not installed in either "C:\Program Files (x86)\GridcoinResearch\" or "C:\Program Files\GridcoinResearch\". The software looks for these 2 paths to launch the wallet once the compression finishes.
