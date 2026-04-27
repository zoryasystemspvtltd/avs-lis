REM Batch File for LIS Print System
REM EXAMPLE lisprint.bat 1903991N638432957411398349.prn \\DESKTOP-L36AN75\LISPRINTER

COPY /B %1 %2
ECHO "COPY /B %1 %2" >> ../log/lisprint_log.txt