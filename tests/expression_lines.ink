== test_knot ==
#dink

// Valid cases (must start with -)
- 2:
- (calculation):
- variable>2:
- (myknotname):

// Normal dialogue (should still work)
FRED: Hello world.
- FRED: Hello world with dash.

// Invalid cases (should error)
- variable>2: Do some action.
- (myknotname): GEORGE (O.S.): Once upon a time.
