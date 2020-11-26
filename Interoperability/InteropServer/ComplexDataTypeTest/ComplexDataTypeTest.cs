namespace Tecan.Sila2.Interop.Server
{
    //[Export(typeof(IComplexDataTypeTest))]
    //[PartCreationPolicy(CreationPolicy.Shared)]
    //internal class ComplexDataTypeTest : UserInterceptorBase, IComplexDataTypeTest
    //{
    //    private readonly Random _random = new Random();

    //    public IRequestInterceptor User => this;

    //    public ICollection<string> HugeList
    //    {
    //        get;
    //    } = Enumerable.Range( 1, 10000 ).Select( i => i.ToString() ).ToList();

    //    public bool Boolean => true;

    //    public ICollection<string> ListString
    //    {
    //        get;
    //    } = new List<string>() { "Foo", "Bar" };

    //    public ICollection<string> RandomChangingListString
    //    {
    //        get
    //        {
    //            var list = new List<string>();
    //            while( list.Count < 6 )
    //            {
    //                var newNumber = _random.Next( 1, 50 ).ToString();
    //                if( !list.Contains( newNumber ) )
    //                {
    //                    list.Add(newNumber);
    //                }
    //            }

    //            return list;
    //        }
    //    }

    //    public IDictionary<string, string> ListStructPairString
    //    {
    //        get;
    //    } = new Dictionary<string, string>()
    //    {
    //        { "Foo", "Bar" },
    //        { "The universal answer", "42" }
    //    };

    //    public StructPairString StructPairString => new StructPairString(new StructPairStringPairString("Foo", "Bar"));

    //    public DataTypeString DataType => new DataTypeString("Foo");

    //    public string ThrowException => throw new TestExceptionException("An entirely unpredictable exception happened.");

    //    public ICollection<string> ListProvider( long listLength, long elementLength )
    //    {
    //        var list = new List<string>();
    //        var format = new string( '0', (int)elementLength );
    //        for( int i = 0; i < listLength; i++ )
    //        {
    //            list.Add(i.ToString(format));
    //        }

    //        return list;
    //    }

    //    public ThreeDimensionalString ThreeDimensionalStruct( ThreeDimensionalStructStructStructStruct structStructStruct )
    //    {
    //        return new ThreeDimensionalString(new List<TwoDimensionalString>()
    //        {
    //            new TwoDimensionalString(new List<OneDimensionalString>()
    //            {
    //                new OneDimensionalString(new List<string>()
    //                {
    //                    structStructStruct.FirstDimension.SecondDimension.ThirdDimension
    //                })
    //            })
    //        });
    //    }

    //    public ThreeDimensionalListThreeDimensionalStructResult ThreeDimensionalList( ThreeDimensionalString listListList )
    //    {
    //        return new ThreeDimensionalListThreeDimensionalStructResult(new ThreeDimensionalListThreeDimensionalStructResultFirstDimension(new ThreeDimensionalListThreeDimensionalStructResultFirstDimensionSecondDimension(string.Join(" ", listListList.Value.SelectMany(twoDim => twoDim.Value.SelectMany(oneDim => oneDim.Value))))));
    //    }

    //    public WhyMakeItSimpleWhenYouCanMakeItComplicatedResponse WhyMakeItSimpleWhenYouCanMakeItComplicated( WhyMakeItSimpleWhenYouCanMakeItComplicatedStructListStruct structListStruct,
    //        ICollection<WhyMakeItSimpleWhenYouCanMakeItComplicatedListStructListItem> listStructList )
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Stream XOREncipher( Stream data, long cipherKey )
    //    {
    //        var cipherByte = (int)cipherKey;
    //        var ms = new MemoryStream();
    //        int lastReadByte;
    //        while( (lastReadByte = data.ReadByte()) != -1 )
    //        {
    //            ms.WriteByte((byte)(lastReadByte ^ cipherByte));
    //        }

    //        return ms;
    //    }

    //    public DynamicObjectProperty ValueForTypeProvider( string type )
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override int Priority => 0;

    //    public override bool IsInterceptRequired( Feature feature, string commandIdentifier )
    //    {
    //        return feature.FullyQualifiedIdentifier == "org.silastandard/test/ComplexDataTypeTest/v1";
    //    }

    //    public override IRequestInterception Intercept( string commandIdentifier, User user )
    //    {
    //        return null;
    //    }

    //    public override bool AppliesToCommands => true;

    //    public override bool AppliesToProperties => true;
    //}
}
