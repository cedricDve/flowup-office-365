﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.8.3928.0.
// 
namespace uuidMaker {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class @event {
        
        private eventHeader headerField;
        
        private eventBody bodyField;
        
        /// <remarks/>
        public eventHeader header {
            get {
                return this.headerField;
            }
            set {
                this.headerField = value;
            }
        }
        
        /// <remarks/>
        public eventBody body {
            get {
                return this.bodyField;
            }
            set {
                this.bodyField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    public partial class eventHeader {
        
        private string uUIDField;
        
        private string sourceEntityIdField;
        
        private string organiserUUIDField;
        
        private string organiserSourceEntityIdField;
        
        private string methodField;
        
        private string originField;
        
        private string versionField;
        
        private System.DateTime timestampField;
        
        /// <remarks/>
        public string UUID {
            get {
                return this.uUIDField;
            }
            set {
                this.uUIDField = value;
            }
        }
        
        /// <remarks/>
        public string sourceEntityId {
            get {
                return this.sourceEntityIdField;
            }
            set {
                this.sourceEntityIdField = value;
            }
        }
        
        /// <remarks/>
        public string organiserUUID {
            get {
                return this.organiserUUIDField;
            }
            set {
                this.organiserUUIDField = value;
            }
        }
        
        /// <remarks/>
        public string organiserSourceEntityId {
            get {
                return this.organiserSourceEntityIdField;
            }
            set {
                this.organiserSourceEntityIdField = value;
            }
        }
        
        /// <remarks/>
        public string method {
            get {
                return this.methodField;
            }
            set {
                this.methodField = value;
            }
        }
        
        /// <remarks/>
        public string origin {
            get {
                return this.originField;
            }
            set {
                this.originField = value;
            }
        }
        
        /// <remarks/>
        public string version {
            get {
                return this.versionField;
            }
            set {
                this.versionField = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime timestamp {
            get {
                return this.timestampField;
            }
            set {
                this.timestampField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    public partial class eventBody {
        
        private string nameField;
        
        private System.DateTime startEventField;
        
        private System.DateTime endEventField;
        
        private string descriptionField;
        
        private string locationField;
        
        /// <remarks/>
        public string name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime startEvent {
            get {
                return this.startEventField;
            }
            set {
                this.startEventField = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime endEvent {
            get {
                return this.endEventField;
            }
            set {
                this.endEventField = value;
            }
        }
        
        /// <remarks/>
        public string description {
            get {
                return this.descriptionField;
            }
            set {
                this.descriptionField = value;
            }
        }
        
        /// <remarks/>
        public string location {
            get {
                return this.locationField;
            }
            set {
                this.locationField = value;
            }
        }
    }
}