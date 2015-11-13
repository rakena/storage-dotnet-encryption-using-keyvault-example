// <copyright file="Program.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

namespace KVGettingStarted
{
    using System;
    using System.IO;
    using System.Threading;
    using Microsoft.Azure;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Core;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    /// <summary>
    /// Demonstrates how to use encryption along with Azure Key Vault integration for the Azure Blob service.
    /// </summary>
    public class Program
    {
       
        const string DefaultSecretName = "KVGettingStartedSecret";

        static void Main(string[] args)
        {
            //Demo option: Encryption or Non-encryption
            Console.WriteLine("Do you want to encrypt the data in Azure Storage? [Y or N] ");

            string input = Console.ReadLine();

            string Keepcontainer = "N";

            if (input.ToUpper() == "N")
            {
                const string DemoContainer = "azurecon-noencryption";
                Console.WriteLine();
                Console.WriteLine("Storing data in Azure storage blob without encryption");

                // Retrieve storage account information from connection string
                // How to create a storage connection string - https://azure.microsoft.com/en-us/documentation/articles/storage-configure-connection-string/
                CloudStorageAccount storageAccount01 = EncryptionShared.Utility.CreateStorageAccountFromConnectionString();

                CloudBlobClient client01 = storageAccount01.CreateCloudBlobClient();
                CloudBlobContainer container01 = client01.GetContainerReference(DemoContainer + Guid.NewGuid().ToString("N"));                

              try
                {

                    container01.Create();

                    CloudBlockBlob blob01 = container01.GetBlockBlobReference("azurecon_noencryption_blob");
                    // Upload the contents to the blob.
                    using (var fileStream =System.IO.File.OpenRead("HellowWorld.txt"))
                    {
                        blob01.UploadFromStream(fileStream);
                    }

                    Console.WriteLine();
                    Console.WriteLine("Uploading sample content to Azure sotrage blob: {0} ", blob01.Name);
                    Console.WriteLine();
                    Console.WriteLine("Do you want to delete the demo blob container? [Y or N] ");
                    Keepcontainer = Console.ReadLine();
                    Console.WriteLine();
                    Console.WriteLine("Press [enter] key to exit");
                    Console.ReadLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
              
                finally
                {
                    if (Keepcontainer.ToUpper() == "Y")
                        container01.DeleteIfExists();
                }

            }
            else
            {
                Console.WriteLine("");
                Console.WriteLine("Azure storage blob encryption with Key Vault integration");
                Console.WriteLine();

                const string DemoContainer = "azurecon-encryption";

                // Get the key ID from App.config if it exists.
                string keyID = CloudConfigurationManager.GetSetting("KeyID");

                // If no key ID was specified, we will create a new secret in Key Vault.
                // To create a new secret, this client needs full permission to Key Vault secrets.
                // Once the secret is created, its ID can be added to App.config. Once this is done,
                // this client only needs read access to secrets.
                if (string.IsNullOrEmpty(keyID))
                {
                    //Console.WriteLine("No secret specified in App.config.");
                    //Console.WriteLine();
                    //Console.WriteLine("Please enter the name of a new secret to create in Key Vault.");
                    //Console.WriteLine();
                    //Console.WriteLine("WARNING: This will delete any existing secret with the same name.");
                    //Console.WriteLine();
                    Console.Write("Name of the sample secret to create [{0}]: ", DefaultSecretName);
                    string newSecretName = Console.ReadLine().Trim();

                    if (string.IsNullOrEmpty(newSecretName))
                    {
                        newSecretName = DefaultSecretName;
                    }

                    // Although it is possible to use keys (rather than secrets) stored in Key Vault, this prevents caching.
                    // Therefore it is recommended to use secrets along with a caching resolver (see below).
                    keyID = EncryptionShared.KeyVaultUtility.SetUpKeyVaultSecret(newSecretName);

                    Console.WriteLine();
                    Console.WriteLine("Created a secret with ID: {0}", keyID);
                    Console.WriteLine();
                    Console.WriteLine("Copy the secret ID to App.config to reuse.");
                    Console.WriteLine();
                }

                // Retrieve storage account information from connection string
                // How to create a storage connection string - https://azure.microsoft.com/en-us/documentation/articles/storage-configure-connection-string/
                CloudStorageAccount storageAccount = EncryptionShared.Utility.CreateStorageAccountFromConnectionString();

                CloudBlobClient client = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = client.GetContainerReference(DemoContainer + Guid.NewGuid().ToString("N"));

                // Construct a resolver capable of looking up keys and secrets stored in Key Vault.
                KeyVaultKeyResolver cloudResolver = new KeyVaultKeyResolver(EncryptionShared.KeyVaultUtility.GetAccessToken);

                // To demonstrate how multiple different types of key can be used, we also create a local key and resolver.
                // This key is temporary and won't be persisted.
                RsaKey rsaKey = new RsaKey("rsakey");
                LocalResolver resolver = new LocalResolver();
                resolver.Add(rsaKey);

                // If there are multiple key sources like Azure Key Vault and local KMS, set up an aggregate resolver as follows.
                // This helps users to define a plug-in model for all the different key providers they support.
                AggregateKeyResolver aggregateResolver = new AggregateKeyResolver()
                    .Add(resolver)
                    .Add(cloudResolver);

                // Set up a caching resolver so the secrets can be cached on the client. This is the recommended usage
                // pattern since the throttling targets for Storage and Key Vault services are orders of magnitude
                // different.
                CachingKeyResolver cachingResolver = new CachingKeyResolver(2, aggregateResolver);

                // Create a key instance corresponding to the key ID. This will cache the secret.
                IKey cloudKey = cachingResolver.ResolveKeyAsync(keyID, CancellationToken.None).GetAwaiter().GetResult();

                try
                {
                    container.Create();
                    int size = 5 * 1024 * 1024;
                    byte[] buffer = new byte[size];

                    Random rand = new Random();
                    rand.NextBytes(buffer);

                    // The first blob will use the key stored in Azure Key Vault.
                    CloudBlockBlob blob = container.GetBlockBlobReference("encryptionkey_blob");

                    // Create the encryption policy using the secret stored in Azure Key Vault to be used for upload.
                    BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(cloudKey, null);

                    // Set the encryption policy on the request options.
                    BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                    Console.WriteLine();
                    Console.WriteLine("The first Azure storage blob will use the key stored in Azure Key Vault");
                    Console.WriteLine();
                    Console.WriteLine("Uploading the encrypted key blob to Azure storage: {0} ", blob.Name);

                    // Upload the encrypted contents to the blob.
                    using (MemoryStream stream = new MemoryStream(buffer))
                    {
                        blob.UploadFromStream(stream, size, null, uploadOptions, null);
                    }

                    // Download the encrypted blob.
                    BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, cachingResolver);

                    // Set the decryption policy on the request options.
                    BlobRequestOptions downloadOptions = new BlobRequestOptions() { EncryptionPolicy = downloadPolicy };

                    Console.WriteLine();
                    Console.WriteLine("Downloading he encrypted key blob which contains the content encryption key");

                    // Download and decrypt the encrypted contents from the blob.
                    using (MemoryStream outputStream = new MemoryStream())
                    {
                        blob.DownloadToStream(outputStream, null, downloadOptions, null);
                    }

                    // Upload second blob using the local key.
                    blob = container.GetBlockBlobReference("encrypteddata_blob");

                    // Create the encryption policy using the local key.
                    uploadPolicy = new BlobEncryptionPolicy(rsaKey, null);

                    // Set the encryption policy on the request options.
                    uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                    Console.WriteLine();
                    Console.WriteLine("Uploading the encrypted sample content blob to Azure storage: {0}", blob.Name);

                    // Upload the encrypted contents to the blob. [Rakesh]: Commneted he original demo code and using a file upload
                    //using (MemoryStream stream = new MemoryStream(buffer))
                    //{
                    //    blob.UploadFromStream(stream, size, null, uploadOptions, null);
                    //}


                    // Upload the encrypted contents to the blob.
                    blob.UploadFromFile("HellowWorld.txt", FileMode.Open, null, uploadOptions, null);

                    // Download the encrypted blob. The same policy and options created before can be used because the aggregate resolver contains both
                    // resolvers and will pick the right one based on the key ID stored in blob metadata on the service.
                    //Console.WriteLine("Downloading the 2nd encrypted blob.");

                    //// Download and decrypt the encrypted contents from the blob.
                    //using (MemoryStream outputStream = new MemoryStream())
                    //{
                    //    blob.DownloadToStream(outputStream, null, downloadOptions, null);
                    //}

                    Console.WriteLine();
                    Console.WriteLine("Do you want to delete the demo blob containers? [Y or N] ");
                    Keepcontainer = Console.ReadLine();
                    Console.WriteLine();
                    Console.WriteLine("Press enter key to exit");
                    Console.ReadLine();
                }
                finally
                {
                    if (Keepcontainer.ToUpper() == "Y")
                        container.DeleteIfExists();
                }
            }
        }
    }
}
