1. Change Connection string within 
	1.1 App.config in Dxi800
	1.2 App.config in DataModel

2. EF Code first Data migration
------------------------------------------------
#### Applied Once - Not required any more ---
Enable-Migrations -ProjectName Lis.Api -StartUpProjectName Lis.Api
Enable-Migrations -ProjectName LIS.DataAccess -StartUpProjectName Lis.Api
Enable-Migrations -ProjectName HIS.Api.Simujlator -StartUpProjectName HIS.Api.Simujlator

#### For Lis.Api
Add-Migration -ProjectName Lis.Api -StartUpProjectName Lis.Api
Update-Database -configuration Lis.Api.DataContextMigrations.Configuration -Verbose -ProjectName Lis.Api -StartUpProjectName Lis.Api


#### For LIS Data
Add-Migration -ProjectName LIS.DataAccess -StartUpProjectName Lis.Api
Update-Database -configuration LIS.DataAccess.Migrations.Configuration -Verbose -ProjectName LIS.DataAccess -StartUpProjectName Lis.Api

#### For HIS.Api.Simujlator
Add-Migration -ProjectName HIS.Api.Simujlator -StartUpProjectName HIS.Api.Simujlator
OR
Add-Migration -ProjectName HIS.Api.Simujlator -StartUpProjectName HIS.Api.Simujlator -IgnoreChanges
Update-Database -configuration HIS.Api.Simujlator.Migrations.Configuration -Verbose -ProjectName HIS.Api.Simujlator -StartUpProjectName HIS.Api.Simujlator

------------------------------------------------
3. Generate script for total db
Update-Database -Script -SourceMigration:0 -configuration LIS.DataModel.Migrations.Configuration -Verbose -ProjectName LIS.DataModel -StartUpProjectName Dxi800


Serial Port Communication testing Mock Data
-------------------------------------------------
Send to LIS from Equipment:Upload
Step 1:
<STX>1H|\^&|||ACCESS^500001|||||LIS||P|1|20021231235959<CR><ETX>20<CR><LF>
<STX>2Q|1|^E2DD9F||ALL||||||||O<CR><ETX>15<CR><LF>
<STX>3L|1|F<CR><ETX>FF<CR><LF>
<EOT>

After that Write <ACK> and click send button four times then you receive step 2 message automatically.

Result:
Step 3:
<STX>1H|\^&|||ACCESS^500001|||||LIS||P|1|20001010131522<CR><ETX>20<CR><LF>
<STX>2P|1|AbelCindy<CR><ETX>20<CR><LF>
<STX>3O|1|E2DD9F|^9^1|^^^AbHBsII|||||||||||Serum||||||||||F<CR><ETX>20<CR><LF>
<STX>4R|1|^^^AbHBsII^1|0.18|uIU/mL||N||F||||20001010113536<CR><ETX>20<CR><LF>
<STX>5L|1|F<CR><ETX>20<CR><LF>
<EOT>


Send to Equipment from LIS:Download
Step 2:Send automatically
<STX>1H|\^&|||LIS|||||||P|1|20021231235959<CR><ETX>EA<CR><LF>
<STX>2P|1|<CR><ETX>BB<CR><LF>
<STX>3O|1|SPEC1234||^^^Ferritin|R||||||A||||Serum<CR><ETX>F8
<CR><LF>
<STX>4L|1|F<CR><ETX>FF<CR><LF>


######## - Generate JSON file for equipment
Add, if not already there, a row of "column Musicians" to the spreadsheet. That is, if you have data in columns such as:

Rory Gallagher      Guitar
Gerry McAvoy        Bass
Rod de'Ath          Drums
Lou Martin          Keyboards
Donkey Kong Sioux   Self-Appointed Semi-official Stomper
Note: you might want to add "Musician" and "Instrument" in row 0 (you might have to insert a row there)

Save the file as a CSV file.

Copy the contents of the CSV file to the clipboard

Go to http://www.convertcsv.com/csv-to-json.htm

Verify that the "First row is column names" checkbox is checked

Paste the CSV data into the content area

Mash the "Convert CSV to JSON" button

With the data shown above, you will now have:

[
  {
    "MUSICIAN":"Rory Gallagher",
    "INSTRUMENT":"Guitar"
  },
  {
    "MUSICIAN":"Gerry McAvoy",
    "INSTRUMENT":"Bass"
  },
  {
    "MUSICIAN":"Rod D'Ath",
    "INSTRUMENT":"Drums"
  },
  {
    "MUSICIAN":"Lou Martin",
    "INSTRUMENT":"Keyboards"
  }
  {
    "MUSICIAN":"Donkey Kong Sioux",
    "INSTRUMENT":"Self-Appointed Semi-Official Stomper"
  }
]
With this simple/minimalistic data, it's probably not required, but with large sets of data, it can save you time and headache in the proverbial long run by checking this data for aberrations and abnormalcy.

Go here: http://jsonlint.com/

Paste the JSON into the content area

Pres the "Validate" button.

If the JSON is good, you will see a "Valid JSON" remark in the Results section below; if not, it will tell you where the problem[s] lie so that you can fix it/them.

#### INsert dummy data for testing
---------------
delete from [dbo].[TestRequestDetails]
delete from [dbo].[PatientDetails]

declare @p int =1000
declare @Name nvarchar(max)
declare @PatientId int
declare @SampleNo nvarchar(max)
declare @ReportStatus int;
declare @Age int
declare @t int
WHILE @p > 0
BEGIN
set @Name = LEFT(cast(newid() as varchar(40)), 6)
set @Age = ABS(CHECKSUM(NEWID()) % 100)
INSERT INTO [dbo].[PatientDetails]
           ([Alias],[Name],[Age],[Gender],[UHID],[Phone],[IsActive],[CreatedBy],[CreatedOn],[DateOfBirth],[SiteId])
     VALUES
           ('Mr',@Name,@Age,'M','111','000',1,'DUMMY',getdate(),getdate(),'1')
	set @PatientId = @@IDENTITY

	set @t = ABS(CHECKSUM(NEWID()) % 10)
	WHILE @t > 0
		BEGIN
		set @SampleNo = LEFT(cast(newid() as varchar(40)), 6)
		set @ReportStatus = @p%7

		INSERT INTO [dbo].[TestRequestDetails]
           ([SampleNo],[HISTestCode],[SampleCollectionDate],[SampleReceivedDate],[SpecimenCode],[SpecimenName],[CreatedBy],[CreatedOn],[ReportStatus],[PatientId])
     VALUES
           (@SampleNo,'DUMMY',getdate(),getdate(),'DUMMY','DUMMY','DUMMY',getdate(),@ReportStatus,@PatientId)

		set @t = @t -1
	END
set @p = @p -1
END
------------------

### Publish angular
ng build --prod

## Angular reinstall
npm uninstall -g @angular/cli
npm cache clean
npm install --save @angular/cli@latest




