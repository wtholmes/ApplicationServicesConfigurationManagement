using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Spatial;

namespace ListServiceManagement.ViewModels
{
    /// <summary>
    /// Updated List Onwer Transfer Documet
    /// </summary>
    public class UpdatedElistOwnerTransfer
    {

        /// <summary>
        /// Request Detail/Description
        /// </summary>
        public String RequestStatusDetail { get; set; }

    }
}