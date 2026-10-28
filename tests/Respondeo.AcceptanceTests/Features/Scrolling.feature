Feature: Scroll landing
	As a reader
	I want each navigation to land me at the right place on the page
	So that I never have to hunt for the content I asked for

Scenario: Deep link to an article lands with that article in view
	Given I open question "prima-q002" at article 3
	Then article 3 should be in the viewport
	And the page should be scrolled below the top

Scenario: Opening a question from the browse page lands on the new content
	Given I open the Summa browse page
	When I expand the first part
	And I scroll the browse page down
	And I open the first question
	Then the page breadcrumb should be in the viewport

Scenario: Jumping to an article from the table of contents brings it into view
	Given I open question "prima-q002"
	When I open article 3 from the table of contents
	Then article 3 should be in the viewport
