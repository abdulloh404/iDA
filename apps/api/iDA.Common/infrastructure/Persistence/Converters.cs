using System.Net;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ida.Infrastructure.Persistence;

public class IpAddressConverter() : ValueConverter<string?, IPAddress?>(
    value => value == null ? null : IPAddress.Parse(value),
    address => address == null ? null : address.ToString());
