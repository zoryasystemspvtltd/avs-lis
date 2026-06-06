# avs-lis
LIS For AVS System

#### For Lis.Api
Add-Migration -ProjectName Lis.Api -StartUpProjectName Lis.Api
Update-Database -configuration Lis.Api.Migrations.Configuration -Verbose -ProjectName Lis.Api -StartUpProjectName Lis.Api


#### For LIS Data
Add-Migration -ProjectName LIS.DataAccess -StartUpProjectName Lis.Api
Update-Database -configuration LIS.DataAccess.Migrations.Configuration -Verbose -ProjectName LIS.DataAccess -StartUpProjectName Lis.Api

#### For HIS.Api.Simujlator
Add-Migration -ProjectName HIS.Api.Simujlator -StartUpProjectName HIS.Api.Simujlator
OR
Add-Migration -ProjectName HIS.Api.Simujlator -StartUpProjectName HIS.Api.Simujlator -IgnoreChanges
Update-Database -configuration HIS.Api.Simujlator.Migrations.Configuration -Verbose -ProjectName HIS.Api.Simujlator -StartUpProjectName HIS.Api.Simujlator

------------------------------------------------
