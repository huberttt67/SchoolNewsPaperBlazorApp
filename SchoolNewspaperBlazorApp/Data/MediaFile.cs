﻿namespace SchoolNewspaperBlazorApp.Data
{
    public class MediaFile
    {
        public int Id { get; set; }
        public required string FileType { get; set; }
        public required string FileName { get; set; }

        // Soft Delete: false = aktywny, true = usunięty logicznie.
        public bool IsDeleted { get; set; }

    }
}
