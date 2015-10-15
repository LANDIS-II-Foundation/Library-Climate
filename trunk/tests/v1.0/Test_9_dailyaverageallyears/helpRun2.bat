rem working directory

set workingdir=K:\Research_Faculty\AFRI_Chippewa_Project\Output_From_Sims\032813_AFRI_PLTCN1-wind2
set homedir=I:\Research\Samba\scheller_lab\Lucash\LANDIS_Input_Files\CNF_input_files-PLot_CN1

if not exist %workingdir%\%1\replicate%2 mkdir %workingdir%\%1\replicate%2
K:
cd %workingdir%\%1\replicate%2
copy I:\Research\Samba\scheller_lab\Lucash\LANDIS_Input_Files\CNF_input_files-PLot_CN1\%1.txt
call landis %1.txt
I:
cd %homedir%
