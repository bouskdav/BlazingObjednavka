using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BlazingObjednavka.Shared
{
    public class Address
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vyplňte, prosím, vaše jméno.")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vyplňte, prosím, ulici a č.p.")]
        [MaxLength(100)]
        public string Line1 { get; set; }

        [MaxLength(100)]
        public string Line2 { get; set; }

        [Required(ErrorMessage = "Vyplňte, prosím, město.")]
        [MaxLength(50)]
        public string City { get; set; }

        [MaxLength(20)]
        public string Region { get; set; }

        [Required(ErrorMessage = "Vyplňte, prosím, PSČ.")]
        [MaxLength(20)]
        public string PostalCode { get; set; }

        [Required(ErrorMessage = "Vyplňte, prosím, váš email.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vyplňte, prosím, váš telefon.")]
        public string Phone { get; set; }
    }
}
