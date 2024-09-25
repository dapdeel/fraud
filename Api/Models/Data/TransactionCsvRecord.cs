using CsvHelper.Configuration.Attributes;

public class TransactionCsvRecord
{
    // Debit Customer Fields
    [Name("debitCustomer_email")]
    public  string DebitCustomerEmail { get; set; }

    [Name("debitCustomer_customerId")]
    public  string DebitCustomerId { get; set; }

    [Name("debitCustomer_name")]
    public  string DebitCustomerName { get; set; }

    [Name("debitCustomer_phone")]
    public  string DebitCustomerPhone { get; set; }

    [Name("debitCustomer_DeviceId")]
    public  string DebitCustomerDeviceId { get; set; }

    [Name("debitCustomer_ipAddress")]
    public  string DebitCustomerIpAddress { get; set; }

    [Name("debitCustomer_deviceType")]
    public  int DebitCustomerDeviceType { get; set; }

    [Name("debitCustomer_accountNumber")]
    public  string DebitCustomerAccountNumber { get; set; }

    [Name("debitCustomer_bankCode")]
    public  string DebitCustomerBankCode { get; set; }

    [Name("debitCustomer_country")]
    public  string DebitCustomerCountry { get; set; }

    // Credit Customer Fields
    [Name("creditCustomer_email")]
    public required string CreditCustomerEmail { get; set; }

    [Name("creditCustomer_customerId")]
    public  string CreditCustomerId { get; set; }

    [Name("creditCustomer_name")]
    public  string CreditCustomerName { get; set; }

    [Name("creditCustomer_phone")]
    public  string CreditCustomerPhone { get; set; }

    [Name("creditCustomer_accountNumber")]
    public  string CreditCustomerAccountNumber { get; set; }

    [Name("creditCustomer_bankCode")]
    public  string CreditCustomerBankCode { get; set; }

    [Name("creditCustomer_country")]
    public  string CreditCustomerCountry { get; set; }

    // Transaction Fields
    [Name("transaction_transactionId")]
    public  string TransactionId { get; set; }

    [Name("transaction_amount")]
    public float TransactionAmount { get; set; }

    [Name("transaction_TransactionDate")]
    public DateTime TransactionDate { get; set; }

    [Name("transaction_description")]
    public  string TransactionDescription { get; set; }

    [Name("transaction_type")]
    public  string TransactionType { get; set; }

    // ObservatoryId
    [Name("ObservatoryId")]
    public int ObservatoryId { get; set; }


    public string ObservatoryTag { get; set; }
}
