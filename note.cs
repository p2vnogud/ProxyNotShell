protected override AnchorMailbox ResolveAnchorMailbox()
{

    if (this.skipTargetBackEndCalculation)
    {
        base.Logger.Set(3, "OrgRelationship-Anonymous");
        return new AnonymousAnchorMailbox(this);
    }

    if (base.UseRoutingHintForAnchorMailbox)
    {
        string text;
        if (RequestPathParser.IsAutodiscoverV2PreviewRequest(base.ClientRequest.Url.AbsolutePath))
        {
            text = base.ClientRequest.Params["Email"];
        }
        else if (RequestPathParser.IsAutodiscoverV2Version1Request(base.ClientRequest.Url.AbsolutePath))
        {
            int num = base.ClientRequest.Url.AbsolutePath.LastIndexOf('/');
            text = base.ClientRequest.Url.AbsolutePath.Substring(num + 1);
        }
        else
        {
            text = this.TryGetExplicitLogonNode(0);
        }

        string text2;
        if (ExplicitLogonParser.TryGetNormalizedExplicitLogonAddress(text, ref text2) && SmtpAddress.IsValidSmtpAddress(text2))
        {
            this.isExplicitLogonRequest = true;
            this.explicitLogonAddress = text;

            //... 
        }
    }
    return base.ResolveAnchorMailbox();
}

protected override UriBuilder GetClientUrlForProxy()
{
    string absoluteUri = base.ClientRequest.Url.AbsoluteUri;
    string uri = absoluteUri;
    if (this.isExplicitLogonRequest && !RequestPathParser.IsAutodiscoverV2Request(base.ClientRequest.Url.AbsoluteUri))
    {
        uri = UrlHelper.RemoveExplicitLogonFromUrlAbsoluteUri(absoluteUri, this.explicitLogonAddress);
    }
    return new UriBuilder(uri);
}






public static bool IsAutodiscoverV2PreviewRequest(string path)
{
    ArgumentValidator.ThrowIfNull("path", path);
    return path.EndsWith("/autodiscover.json", StringComparison.OrdinalIgnoreCase);
}

public static bool IsAutodiscoverV2Request(string path)
{
    ArgumentValidator.ThrowIfNull("path", path);
    return RequestPathParser.IsAutodiscoverV2Version1Request(path) || RequestPathParser.IsAutodiscoverV2PreviewRequest(path);
}


public static string RemoveExplicitLogonFromUrlAbsoluteUri(string absoluteUri, string explicitLogonAddress) { 
    ArgumentValidator.ThrowIfNull("absoluteUri", absoluteUri); 
    ArgumentValidator.ThrowIfNull("explicitLogonAddress", explicitLogonAddress); 
    string text = "/" + explicitLogonAddress; 
    int num = absoluteUri.IndexOf(text); 
    if (num != -1) { 
        return absoluteUri.Substring(0, num) + absoluteUri.Substring(num + text.Length); 
    } 
    return absoluteUri; 
}




private void ReadProperties(PSObject dso)
{
    dso.isDeserialized = true;
    dso.adaptedMembers = new PSMemberInfoInternalCollection<PSPropertyInfo>();
    // ...
    if (this.ReadStartElementAndHandleEmpty("Props"))
    {
        while (this._reader.NodeType == XmlNodeType.Element)
        {
            string name = this.ReadNameAttribute();
            object serializedValue = this.ReadOneObject();
            PSProperty member = new PSProperty(name, serializedValue);
            dso.adaptedMembers.Add(member);
        }
        this.ReadEndElement();
    }
}



internal object ReadOneObject(out string streamName)
{
    this.CheckIfStopping();
    object result;
    try
    {
        this.depthBelowTopLevel++;
        if (this.depthBelowTopLevel == 50)
        {
            throw this.NewXmlException(Serialization.DeserializationTooDeep, null, new object[0]);
        }
        bool flag;
        object obj = this.ReadOneDeserializedObject(out streamName, out flag);
        if (obj == null)
        {
            result = null;
        }
        else
        {
            if (!flag)
            {
                PSObject psobject = PSObject.AsPSObject(obj);
                if (Deserializer.IsDeserializedInstanceOfType(psobject, typeof(CimInstance)))
                {
                    return this.RehydrateCimInstance(psobject);
                }
                Type targetTypeForDeserialization = psobject.GetTargetTypeForDeserialization(this._typeTable); // [1]
                if (null != targetTypeForDeserialization)
                {
                    Exception ex = null;
                    try
                    {
                        object obj2 = LanguagePrimitives.ConvertTo(obj, targetTypeForDeserialization, true, CultureInfo.InvariantCulture, this._typeTable); // [2]
                        PSEtwLog.LogAnalyticVerbose(PSEventId.Serializer_RehydrationSuccess, PSOpcode.Rehydration, PSTask.Serialization, PSKeyword.Serializer, new object[]
                        {
                            psobject.InternalTypeNames.Key,
                            targetTypeForDeserialization.FullName,
                            obj2.GetType().FullName
                        });
                        return obj2;
                    }
                    catch (InvalidCastException ex)
                    {
                    }
                    catch (ArgumentException ex)
                    {
                    }
                    PSEtwLog.LogAnalyticError(PSEventId.Serializer_RehydrationFailure, PSOpcode.Rehydration, PSTask.Serialization, PSKeyword.Serializer, new object[]
                    {
                        psobject.InternalTypeNames.Key,
                        targetTypeForDeserialization.FullName,
                        ex.ToString(),
                        (ex.InnerException == null) ? string.Empty : ex.InnerException.ToString()
                    });
                }
            }
            result = obj;
        }
    }
    finally
    {
        this.depthBelowTopLevel--;
    }
    return result;
}


internal object ReadOneObject(out string streamName)
{
    this.CheckIfStopping();
    object result;
    try
    {
        this.depthBelowTopLevel++;
        if (this.depthBelowTopLevel == 50)
        {
            throw this.NewXmlException(Serialization.DeserializationTooDeep, null, new object[0]);
        }
        bool flag;
        object obj = this.ReadOneDeserializedObject(out streamName, out flag); // [1]
        if (obj == null)
        {
            result = null;
        }
        else // [2]
        {
            ...
            ...
        }
    }
}




if (this.IsNextElement("Obj"))
{
    InternalDeserializer._trace.WriteLine("PSObject Element");
    return this.ReadPSObject();
}





private PSObject ReadPSObject()
{
    PSObject psobject = this.ReadAttributeAndCreatePSObject();
    if (!this.ReadStartElementAndHandleEmpty("Obj"))
    {
        return psobject;
    }
    bool overrideTypeInfo = true;
    while (this._reader.NodeType == XmlNodeType.Element)
    {
        if (this.IsNextElement("TN") || this.IsNextElement("TNRef"))
        {
            this.ReadTypeNames(psobject);
            overrideTypeInfo = false;
        }
        else if (this.IsNextElement("Props"))
        {
            this.ReadProperties(psobject);
        }
        else if (this.IsNextElement("MS"))
        {
            this.ReadMemberSet(psobject.InstanceMembers);
        }
        // ...
    }
}




internal Type GetTargetTypeForDeserialization(TypeTable backupTypeTable)
{
    PSMemberInfo psstandardMember = this.GetPSStandardMember(backupTypeTable, "TargetTypeForDeserialization");
    if (psstandardMember != null)
    {
        return psstandardMember.Value as Type;
    }
    return null;
}