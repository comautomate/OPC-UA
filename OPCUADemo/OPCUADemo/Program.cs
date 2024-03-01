using Opc.Ua.Client;
using Opc.Ua;
using Opc.Ua.Configuration;

namespace OPCUADemo
{
    internal class Program
    {
        static SessionReconnectHandler? m_reconnectHandler;
        static Session? m_session;
        static async Task Main(string[] args)
        {
            ApplicationInstance application = new ApplicationInstance();
            application.ApplicationType = Opc.Ua.ApplicationType.Client;
            application.ConfigSectionName = "Client";
            application.LoadApplicationConfiguration(false).Wait();
            application.CheckApplicationInstanceCertificate(false,0).Wait();

            var m_configuration = application.ApplicationConfiguration;
            m_configuration.CertificateValidator.CertificateValidation += CertificateValidator_CertificateValidation;
            string serverUrl = "opc.tcp://192.168.0.200:4840";

            var endpointDescription = CoreClientUtils.SelectEndpoint(m_configuration, serverUrl, true, 15000);
            var endpointConfiguration = EndpointConfiguration.Create(m_configuration);
            var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

            m_session = await Session.Create(
                m_configuration, 
                endpoint, 
                false, 
                true, 
                m_configuration.ApplicationName, 
                60000, 
                null, 
                null);
            m_session.KeepAlive += Session_KeepAlive;
            m_reconnectHandler = new SessionReconnectHandler(true, 10 * 1000);

            ReferenceDescription testValueRef = GetReference("DEMOPLC.DataBlocksGlobal.demoblock.demotag7");

            ReadValueId nodeToRead = new ReadValueId();
            nodeToRead.NodeId = (NodeId)testValueRef.NodeId;
            nodeToRead.AttributeId = Attributes.Value;

            ReadValueIdCollection nodesToRead = new ReadValueIdCollection();
            nodesToRead.Add(nodeToRead);

            DataValueCollection readResults = null;
            DiagnosticInfoCollection readDiagnosticInfos = null;

            ResponseHeader readHeader = m_session.Read(null, 0, TimestampsToReturn.Neither, nodesToRead, out readResults, out readDiagnosticInfos);

            ClientBase.ValidateResponse(readResults, nodesToRead);
            ClientBase.ValidateDiagnosticInfos(readDiagnosticInfos, nodesToRead);

            var m_value = readResults[0];

            WriteValue valueToWrite = new WriteValue();

            valueToWrite.NodeId = (NodeId)testValueRef.NodeId;
            valueToWrite.AttributeId = Attributes.Value;
            valueToWrite.Value.Value = ChangeType(m_value, "demo");
            valueToWrite.Value.StatusCode = StatusCodes.Good;
            valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
            valueToWrite.Value.SourceTimestamp = DateTime.MinValue;

            WriteValueCollection valuesToWrite = new WriteValueCollection();
            valuesToWrite.Add(valueToWrite);

            StatusCodeCollection writeResults = null;
            DiagnosticInfoCollection writeDiagnosticInfos = null;

            ResponseHeader writeHeader = m_session.Write(null, valuesToWrite, out writeResults, out writeDiagnosticInfos);

            ClientBase.ValidateResponse(writeResults, valuesToWrite);
            ClientBase.ValidateDiagnosticInfos(writeDiagnosticInfos, valuesToWrite);

            m_session.Close();
        }

        private static object ChangeType(DataValue m_value, object value)
        {
            object result = (m_value != null) ? m_value.Value : null;

            switch (m_value.WrappedValue.TypeInfo.BuiltInType)
            {
                case BuiltInType.Boolean:
                    {
                        result = Convert.ToBoolean(value);
                        break;
                    }

                case BuiltInType.SByte:
                    {
                        result = Convert.ToSByte(value);
                        break;
                    }

                case BuiltInType.Byte:
                    {
                        result = Convert.ToByte(value);
                        break;
                    }

                case BuiltInType.Int16:
                    {
                        result = Convert.ToInt16(value);
                        break;
                    }

                case BuiltInType.UInt16:
                    {
                        result = Convert.ToUInt16(value);
                        break;
                    }

                case BuiltInType.Int32:
                    {
                        result = Convert.ToInt32(value);
                        break;
                    }

                case BuiltInType.UInt32:
                    {
                        result = Convert.ToUInt32(value);
                        break;
                    }

                case BuiltInType.Int64:
                    {
                        result = Convert.ToInt64(value);
                        break;
                    }

                case BuiltInType.UInt64:
                    {
                        result = Convert.ToUInt64(value);
                        break;
                    }

                case BuiltInType.Float:
                    {
                        result = Convert.ToSingle(value);
                        break;
                    }

                case BuiltInType.Double:
                    {
                        result = Convert.ToDouble(value);
                        break;
                    }

                default:
                    {
                        result = value;
                        break;
                    }
            }

            return result;
        }
        private static ReferenceDescription GetReference(string value)
        {
            ReferenceDescription? currentDescription = null;
            string[] values = value.Split('.');
            ReferenceDescriptionCollection collection = MyBrowse(ObjectIds.ObjectsFolder);
            for (int i = 0; i < values.Length; i++)
            {
                currentDescription = collection.Where(e => e.DisplayName == values[i]).FirstOrDefault();
                collection = MyBrowse((NodeId)currentDescription.NodeId);
            }
            return currentDescription;
        }
        private static ReferenceDescriptionCollection MyBrowse(NodeId sourceId)
        {
            BrowseDescription nodeToBrowse1 = new BrowseDescription();

            nodeToBrowse1.NodeId = sourceId;
            nodeToBrowse1.BrowseDirection = BrowseDirection.Forward;
            nodeToBrowse1.ReferenceTypeId = ReferenceTypeIds.Aggregates;
            nodeToBrowse1.IncludeSubtypes = true;
            nodeToBrowse1.NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable);
            nodeToBrowse1.ResultMask = (uint)BrowseResultMask.All;

            // find all nodes organized by the node.
            BrowseDescription nodeToBrowse2 = new BrowseDescription();

            nodeToBrowse2.NodeId = sourceId;
            nodeToBrowse2.BrowseDirection = BrowseDirection.Forward;
            nodeToBrowse2.ReferenceTypeId = ReferenceTypeIds.Organizes;
            nodeToBrowse2.IncludeSubtypes = true;
            nodeToBrowse2.NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable);
            nodeToBrowse2.ResultMask = (uint)BrowseResultMask.All;

            BrowseDescriptionCollection nodesToBrowse = new BrowseDescriptionCollection();
            nodesToBrowse.Add(nodeToBrowse1);
            nodesToBrowse.Add(nodeToBrowse2);

            // fetch references from the server.
            ReferenceDescriptionCollection references = Browse(m_session, nodesToBrowse, false);

            for (int ii = 0; ii < references.Count; ii++)
            {
                //ReferenceDescription target = references[ii];
            }
            return references;
        }

        /// <summary>
        /// Browses the address space and returns the references found.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="nodesToBrowse">The set of browse operations to perform.</param>
        /// <param name="throwOnError">if set to <c>true</c> a exception will be thrown on an error.</param>
        /// <returns>
        /// The references found. Null if an error occurred.
        /// </returns>
        public static ReferenceDescriptionCollection Browse(Session session, BrowseDescriptionCollection nodesToBrowse, bool throwOnError)
        {
            try
            {
                ReferenceDescriptionCollection references = new ReferenceDescriptionCollection();
                BrowseDescriptionCollection unprocessedOperations = new BrowseDescriptionCollection();

                while (nodesToBrowse.Count > 0)
                {
                    // start the browse operation.
                    BrowseResultCollection results = null;
                    DiagnosticInfoCollection diagnosticInfos = null;

                    session.Browse(
                        null,
                        null,
                        0,
                        nodesToBrowse,
                        out results,
                        out diagnosticInfos);

                    ClientBase.ValidateResponse(results, nodesToBrowse);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToBrowse);

                    ByteStringCollection continuationPoints = new ByteStringCollection();

                    for (int ii = 0; ii < nodesToBrowse.Count; ii++)
                    {
                        // check for error.
                        if (StatusCode.IsBad(results[ii].StatusCode))
                        {
                            // this error indicates that the server does not have enough simultaneously active 
                            // continuation points. This request will need to be resent after the other operations
                            // have been completed and their continuation points released.
                            if (results[ii].StatusCode == StatusCodes.BadNoContinuationPoints)
                            {
                                unprocessedOperations.Add(nodesToBrowse[ii]);
                            }

                            continue;
                        }

                        // check if all references have been fetched.
                        if (results[ii].References.Count == 0)
                        {
                            continue;
                        }

                        // save results.
                        references.AddRange(results[ii].References);

                        // check for continuation point.
                        if (results[ii].ContinuationPoint != null)
                        {
                            continuationPoints.Add(results[ii].ContinuationPoint);
                        }
                    }

                    // process continuation points.
                    ByteStringCollection revisedContinuationPoints = new ByteStringCollection();

                    while (continuationPoints.Count > 0)
                    {
                        // continue browse operation.
                        session.BrowseNext(
                            null,
                            false,
                            continuationPoints,
                            out results,
                            out diagnosticInfos);

                        ClientBase.ValidateResponse(results, continuationPoints);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos, continuationPoints);

                        for (int ii = 0; ii < continuationPoints.Count; ii++)
                        {
                            // check for error.
                            if (StatusCode.IsBad(results[ii].StatusCode))
                            {
                                continue;
                            }

                            // check if all references have been fetched.
                            if (results[ii].References.Count == 0)
                            {
                                continue;
                            }

                            // save results.
                            references.AddRange(results[ii].References);

                            // check for continuation point.
                            if (results[ii].ContinuationPoint != null)
                            {
                                revisedContinuationPoints.Add(results[ii].ContinuationPoint);
                            }
                        }

                        // check if browsing must continue;
                        continuationPoints = revisedContinuationPoints;
                    }

                    // check if unprocessed results exist.
                    nodesToBrowse = unprocessedOperations;
                }

                // return complete list.
                return references;
            }
            catch (Exception exception)
            {
                if (throwOnError)
                {
                    throw new ServiceResultException(exception, StatusCodes.BadUnexpectedError);
                }

                return null;
            }
        }

        private static void Session_KeepAlive(ISession session, KeepAliveEventArgs e)
        {
            if (ServiceResult.IsBad(e.Status))
            {
                m_reconnectHandler.BeginReconnect(m_session, 1000, Server_ReconnectComplete);
            }
        }

        private static void Server_ReconnectComplete(object sender, EventArgs e)
        {
            if (m_reconnectHandler.Session != null)
            {
                if (!ReferenceEquals(m_session, m_reconnectHandler.Session))
                {
                    var session = m_session;
                    session.KeepAlive -= Session_KeepAlive;
                    m_session = m_reconnectHandler.Session as Session;
                    m_session.KeepAlive += Session_KeepAlive;
                    Utils.SilentDispose(session);
                }
            }
        }

        private static void CertificateValidator_CertificateValidation(Opc.Ua.CertificateValidator sender, Opc.Ua.CertificateValidationEventArgs e)
        {
            e.Accept = true;
        }
    }
}
