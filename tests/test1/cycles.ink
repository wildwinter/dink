VAR testInt = 0
VAR testString = ""
LIST testList = item1, item2

=== Cycles
#dink
-> LineTest

= LineTest
// This should allow tests inside a bracketed section
{
- testInt==1:
    FRED: This should be fine. #id:cycles_Cycles_LineTest_65J9
- testInt==2:
    GEORGE: So should this. #id:cycles_Cycles_LineTest_XFQW
}
-> DONE

= FancyBarkTest
// How does the bracket (1/6) etc. work on this?
{stopping:
- FRED: Fancy Bark 1 #id:cycles_Cycles_FancyBarkTest_RR4G
- FRED: Fancy Bark 2 #id:cycles_Cycles_FancyBarkTest_D4KV
- FRED: Fancy Bark 3 #id:cycles_Cycles_FancyBarkTest_A2I1
- FRED: Fancy Bark 4 #id:cycles_Cycles_FancyBarkTest_3KK1
-   {shuffle:
        - FRED: Spinning on fancy bark 5 #id:cycles_Cycles_FancyBarkTest_FF35
        - FRED: Spinning on fancy bark 6 #id:cycles_Cycles_FancyBarkTest_23Q8
    }
}
-> DONE

= StringExpressionsTest
Check: #id:cycles_Cycles_StringExpressionsTest_Y7QJ
{testString:
- "test":
    GEORGE: Huh. #id:cycles_Cycles_StringExpressionsTest_1L9A
- "test":
    FRED: Huh yourself. #id:cycles_Cycles_StringExpressionsTest_ZHNZ
}
-> DONE

= ListExpressionTest
Check: #id:cycles_Cycles_ListExpressionTest_EWNK
{testList:
- item1:
    GEORGE: List item 1. #id:cycles_Cycles_ListExpressionTest_LUCG
- (item2):
    GEORGE: List item 2. #id:cycles_Cycles_ListExpressionTest_JXXD
}
-> DONE