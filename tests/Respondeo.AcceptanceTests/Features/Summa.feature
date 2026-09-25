Feature: Summa browse state
  As a visitor browsing the Summa Theologiae
  I want my search, expanded sections, and scroll position remembered when I go back
  So that returning from a question drops me exactly where I left off

  Scenario: Pressing back restores an expanded part and scroll position
	Given I open the Summa browse page
	When I expand the first part
	And I scroll the browse page down
	And I open the first question
	And I press the browser back button
	Then the first part should be expanded
	And the browse page should be scrolled down

  Scenario: Pressing back restores the search results I left
	Given I open the Summa browse page
	When I search the Summa for "God"
	And I open the first search result
	And I press the browser back button
	Then the Summa search box should contain "God"
	And search results should be shown

  Scenario: Leaving the Summa area resets the browse state
	Given I open the Summa browse page
	When I search the Summa for "God"
	And I navigate to the home page
	And I open the Summa browse page
	Then the Summa search box should be empty
