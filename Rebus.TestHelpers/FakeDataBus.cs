﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rebus.DataBus;
using Rebus.DataBus.InMem;
using Rebus.Testing;
using InMemDataBusStorage = Rebus.TestHelpers.Internals.InMemDataBusStorage;

namespace Rebus.TestHelpers
{
    /// <summary>
    /// Test helper that can be used to fake the presence of a configured data bus, using the given in-mem data store to store data
    /// </summary>
    public class FakeDataBus : IDataBus
    {
        readonly IDataBusStorage _dataBusStorage;

        /// <summary>
        /// Establishes a fake presence of a configured data bus, using the given <see cref="InMemDataStore"/> to retrieve data
        /// </summary>
        public static IDisposable EstablishContext(InMemDataStore dataStore)
        {
            if (dataStore == null) throw new ArgumentNullException(nameof(dataStore));

            TestBackdoor.EnableTestMode(new InMemDataBusStorage(dataStore));

            return new CleanUp(TestBackdoor.Reset);
        }

        /// <summary>
        /// Creates the fake data bus, optionally using the given in-mem data store to store attachments
        /// </summary>
        /// <param name="dataStore"></param>
        public FakeDataBus(InMemDataStore dataStore = null)
        {
            // if a data store was passed in, we use that
            if (dataStore != null)
            {
                _dataBusStorage = new InMemDataBusStorage(dataStore);
            }
            // otherwise, if there is an "ambient" storage, use that
            else if (TestBackdoor.TestDataBusStorage != null)
            {
                _dataBusStorage = TestBackdoor.TestDataBusStorage;
            }
            // last resort: just fake it in mem
            else
            {
                _dataBusStorage = new InMemDataBusStorage(new InMemDataStore());
            }
        }

        /// <inheritdoc />
        public async Task<DataBusAttachment> CreateAttachment(Stream source, Dictionary<string, string> optionalMetadata = null)
        {
            var id = Guid.NewGuid().ToString();

            await _dataBusStorage.Save(id, source, optionalMetadata).ConfigureAwait(false);

            return new DataBusAttachment(id);
        }

        /// <inheritdoc />
        public async Task<Stream> OpenRead(string dataBusAttachmentId)
        {
            return await _dataBusStorage.Read(dataBusAttachmentId).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, string>> GetMetadata(string dataBusAttachmentId)
        {
            return await _dataBusStorage.ReadMetadata(dataBusAttachmentId).ConfigureAwait(false);
        }

        class CleanUp : IDisposable
        {
            readonly Action _disposeAction;

            public CleanUp(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }

            public void Dispose()
            {
                _disposeAction();
            }
        }
    }
}