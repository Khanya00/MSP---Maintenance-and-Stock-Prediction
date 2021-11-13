using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MSP.Models
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [DisplayName("Product ID")]
        public int ProductId { get; set; }

        [StringLength(255)]
        [DisplayName("Product")]
        [Required(ErrorMessage = "This Field is Required")]
        public string ProductName { get; set; }

        
        [DisplayName("Units Per Case")]
        [Required(ErrorMessage = "This Field is Required")]
        public double? UnitsPerCase { get; set; }

        
        [DisplayName("Units Per Pack")]
        [Required(ErrorMessage = "This Field is Required")]
        public double? UnitsPerPack { get; set; }

        
        [DisplayName("Shelf Life (days)")]
        [Required(ErrorMessage = "This Field is Required")]
        public double? ShelfLife { get; set; }

        [StringLength(255)]
        [DisplayName("Product Category")]
        [Required(ErrorMessage = "This Field is Required")]
        public string DepartmentName { get; set; }

        [StringLength(255)]
        [DisplayName("Product Type")]
        [Required(ErrorMessage = "This Field is Required")]
        public string ProductTypeName { get; set; }
    }
}
