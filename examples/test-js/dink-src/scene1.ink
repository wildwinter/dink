=== Scene1
#dink
Here we are in scene1.ink - this is testing a set of different blocks. #id:scene1_Scene1_R819
-> Part1

= Part1
FRED: This is a block called Part1 in a scene. #id:scene1_Scene1_Part1_S494
FRED: It consists of a run of lines. #id:scene1_Scene1_Part1_ICIG
And this, here, is an Ink Action, not a line. #id:scene1_Scene1_Part1_621G
Actions aren't localised unless you turn on locActionBeats #id:scene1_Scene1_Part1_LTDB
+ [Go to Part2 #id:scene1_Scene1_Part1_VXIU]
    -> Part2

// This comment should appear on Part2
= Part2
You saunter into Part2 #id:scene1_Scene1_Part2_N5RW
{stopping:
- This is your first visit. #id:scene1_Scene1_Part2_HQUO
- This is your second visit. #id:scene1_Scene1_Part2_3AHR
-
    {shuffle:
    - This is one type of random visit. #id:scene1_Scene1_Part2_NR0K
    - This is another type of random visit #id:scene1_Scene1_Part2_FLIK
    }
}
+ [Go to Part3 #id:scene1_Scene1_Part2_CF6W]
    -> Part3

= Part3
// This is part 3
Dave walks into the room. #id:scene1_Scene1_Part3_UZOH
DAVE: (quietly) Let's see if this works, shall we? #id:scene1_Scene1_Part3_9MXL
+ [Go to Part4 #id:scene1_Scene1_Part3_YTMH]
    -> Part4

= Part4
// There is a choice here.
GEORGE: Which way would you like to go? #id:scene1_Scene1_Part4_NY6V
+ [Go right. #id:scene1_Scene1_Part4_T9GZ]
    FRED: I'd like to go right! #id:scene1_Scene1_Part4_F0PF
    -> Right
+ [Go left. #id:scene1_Scene1_Part4_9L7I]
    FRED: I'd like like to go left! #id:scene1_Scene1_Part4_DNII
    -> Left
+ [Skip it. #id:scene1_Scene1_Part4_Q8FK]
    FRED: Carry on then. #id:scene1_Scene1_Part4_AJDP
-
GEORGE: Okay. #id:scene1_Scene1_Part4_0YY1
+ [Back #id:scene1_Scene1_Part4_PZV1]
    -> Main

= Right
George swerves the car right. #id:scene1_Scene1_Right_3V6T
GEORGE: (upset) You sure you want to go right? #id:scene1_Scene1_Right_WM69
+ [Back #id:scene1_Scene1_Right_P8FP]
    -> Main

= Left
George swerves the car left. #id:scene1_Scene1_Left_HZ7B
GEORGE: (upset) You sure you want to go left? #id:scene1_Scene1_Left_MIM6
+ [(Back.) #id:scene1_Scene1_Left_WXCN]
    -> Main

== OtherContent
This content is nothing at all to do with Dink! #id:scene1_Scene1_OtherContent_FSDK
+ [(Back.) #id:scene1_OtherContent_VZWQ]
    -> Main