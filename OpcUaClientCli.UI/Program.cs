




using OpcUaClientCli.UI;

var app = new App();

if(args.Length > 0) {
    await app.Start(args[0]);
} else {
    await app.Start();
}