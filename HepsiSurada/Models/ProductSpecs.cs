//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HepsiSurada.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class ProductSpecs
    {
        public int ID { get; set; }
        public Nullable<int> ProductID { get; set; }
        public string SpecKey { get; set; }
        public string SpecValue { get; set; }
    
        public virtual Products Products { get; set; }
    }
}