using System;
using System.Collections.Generic;

namespace IMS.DAL.PrimaryDBContext;

public partial class ProductImage
{
    public long ImageId { get; set; }

    public string ImageName { get; set; } = null!;

    public byte[] ImageContent { get; set; } = null!;

    public long ProductIdFk { get; set; }

    public long IsDefault { get; set; }
}
