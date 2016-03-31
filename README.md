# Encrypt the data in azure storage using Azure Key Vault
This sample application demonstrates how to encrypt the data stored in a storage blob using the encryption keys stored in Azure Key Vault.
## How to run this sample
To run this sample, you will need:
  •	Visual Studio 2013
  •	An Internet connection
  •	An Azure subscription (a free trial is sufficient)
  •	Azure Storage Explorer
Every Azure subscription has an associated Azure Active Directory tenant. If you don't already have an Azure subscription, you can get a free subscription by signing up at https://azure.microsoft.com. All of the Azure AD features used by this sample are available free of charge.


## Step 1: Create an Azure Key Vault
Follow the https://azure.microsoft.com/en-us/documentation/articles/key-vault-get-started/ documentation for creating a Key Vault under your subscription.

## Step 2: Create a Storage Account
You can follow the https://azure.microsoft.com/en-us/documentation/articles/storage-create-storage-account/ documentation for creating a storage account. 

## Step 3: Clone or download this repository
From your shell (ie: Git Bash, etc.) or command line, run the following command:
git clone https://github.com/rakena/storage-dotnet-encryption-using-keyvault-example.git

## Step 4: Edit, build, and run the sample in Visual Studio 2013
After you clone or download the sample app, you will need to update the App.config file with following details.

<!--Uncomment the string and insert your storage account name and key in the line below.-->
<add key="StorageConnectionString" value="DefaultEndpointsProtocol=https;AccountName=<>;AccountKey=<>" />
<!--Uncomment the strings and insert your Key Vault credentials in the lines below. For more information about getting started with Key Vault, please look at http://azure.microsoft.com/en-us/documentation/articles/key-vault-get-started/ -->
<add key="KVClientId" value="<>" />
<add key="KVClientKey" value="<>" />
<add key="VaultUri" value="<>"/>


After completing these steps you should be able to successfully build and run the application, which will present a console UI which you can use for testing. 


## About the code
Coming soon...
## More information
Coming soon...
