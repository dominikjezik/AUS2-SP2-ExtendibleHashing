using AUS.Tester;

Console.WriteLine("HeapFile / ExtendibleHashFile Tester");

const string FileName = @"C:\Users\dominik\Desktop\TEST.dat";
//const string FileName = "/Users/dominik/Desktop/TEST.dat";


// HeapFile

/*
var tester1 = new HeapFileTester(FileName, 28000, false);

tester1.CleanAndResetBeforeTest();

//tester1.TestRandomDataSet(3000, 0.6);

tester1.TestFullInsertThenFullDelete();
*/


/*
for (var i = 0.3; i <= 1; i += 0.1)
{
    var tester1 = new HeapFileTester(FileName, 28000, false);

    tester1.CleanAndResetBeforeTest();

    tester1.TestRandomDataSet(3000, i);
}
*/



// ExtendibleHashFile



File.Delete(FileName);
File.Delete(@"C:\Users\dominik\Desktop\TEST.dat");

/*
for (var i = 0.3; i <= 1; i += 0.1)
{
    var tester1 = new ExtendibleHashFileTester(FileName, 28000, false);

    tester1.CleanAndResetBeforeTest();

    tester1.TestRandomDataSet(3000, i);
    //tester1.TestIncreasingKeyAndRandomDataSet(1500, 1);
}
*/

for (int k = 1; k <= 3; k++)
{
    for (var i = 0.3; i <= 1; i += 0.1)
    {
        var tester1 = new ExtendibleHashFileTester(FileName, 28000, false);

        tester1.CleanAndResetBeforeTest();

        tester1.TestRandomDataSet(k*1000, i);
        //tester1.TestIncreasingKeyAndRandomDataSet(1500, 1);
    }
}


/*
var tester1 = new ExtendibleHashFileTester(FileName, 28000, false);

tester1.CleanAndResetBeforeTest();

//tester1.TestRandomDataSet(10, 0.8);
//tester1.TestFullInsertThenFullDelete();
//tester1.TestIncreasingKeyAndRandomDataSet(1000, 0.7);
*/

