# Gridcoin Snapshot Generator
This is a tool that creates a snapshot of the Gridcoin Blockchain. The software works as follows:
1. It will first close the wallet properly to prevent corruption.
2. It will launch 7zip and compress the txleveldb and the blk0001.dat file
3. Once the compression is finished, it will launch the wallet again.

# Timer
You can specify the amount of hours to wait between snapshots. It will use the specified filename with the date and time appended to it at the end.

# Filename
The filename format is as follows:  
<Your Specified Name> Year-Month-Day Hour-Minutes-Seconds AM/PM.7z

# Requisites
You must have 7-zip installed in the C:\Program Files\7-zip folder. This software launches the 7z.exe command line software to compress the blockchain.
