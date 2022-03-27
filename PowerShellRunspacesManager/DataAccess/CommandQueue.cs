namespace PowerShellRunspaceManager
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("CommandQueue")]
    public partial class CommandQueue
    {
        [Key]
        public long CommandID { get; set; }

        [Required]
        [StringLength(50)]
        public string TargetService { get; set; }

        [Required]
        [StringLength(50)]
        public string CommandState { get; set; }

        [Required]
        public string AsyncCommand { get; set; }

        public DateTime SubmitTime { get; set; }

        public DateTime? CompletionTime { get; set; }
    }
}