Feature: Bible reader
  As a reader of Scripture
  I want to open a chapter and see the verses I was pointed to
  So that I can read a passage in context

  Scenario: A chapter opens with its verses
	When I open the Bible chapter "john/3"
	Then I should see verse 16 of the chapter

  Scenario: A single verse is highlighted
	When I open the Bible chapter "john/3?v=16"
	Then only verses "16" should be highlighted

  Scenario: Several verse ranges are highlighted
	When I open the Bible chapter "john/3?v=5-7,16"
	Then only verses "5,6,7,16" should be highlighted

  Scenario: An invalid verse selection still shows the chapter
	When I open the Bible chapter "john/3?v=x"
	Then I should see verse 16 of the chapter
	And only verses "" should be highlighted

  Scenario: The pager moves to the next chapter
	When I open the Bible chapter "john/3"
	And I follow the next chapter link
	Then the address bar should end with "bible/john/4"

  Scenario: An unknown chapter shows a not-found message
	When I open the Bible chapter "john/999"
	Then I should see the chapter not found message
