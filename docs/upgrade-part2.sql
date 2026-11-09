-- Run against the existing Part 2 database before starting the Part 3 API.
IF COL_LENGTH('dbo.Events', 'BannerUrl') IS NULL
    ALTER TABLE dbo.Events ADD BannerUrl nvarchar(2048) NULL;
IF COL_LENGTH('dbo.Users', 'ProfileImageUrl') IS NULL
    ALTER TABLE dbo.Users ADD ProfileImageUrl nvarchar(2048) NULL;
